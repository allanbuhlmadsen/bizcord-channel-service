# Failure points — Channel Service

Known and suspected ways this service can fail, written as preparation for adding reliability measures. Findings marked **measured** were reproduced against the running service; the rest come from reading the code.

## What this service depends on

Two things live outside the service's own process: RabbitMQ, reached over the network, and the database. The database currently runs in memory inside the service, so it cannot be slow or unavailable today — the points below note where that would change with a real one.

The service makes no synchronous calls to other microservices. The Messaging Service calls it, not the other way around, as `docs/interactions.md` describes. There is therefore no outbound HTTP call to protect with a circuit breaker, and the out-of-process dependency that matters is the broker.

## Service dependencies

### 1. A broker outage makes channel creation fail after the channel has already been stored — measured

`ChannelAppService.CreateAsync` saves the channel, then publishes `ChannelCreated`. Both happen inside the HTTP request.

With RabbitMQ stopped, `POST /channels` returned `500` after 11 seconds. A following `GET /channels` showed the channel was there. The log shows the save succeeding and the publish throwing:

    Saved 1 entities to in-memory store.
    System.Threading.Tasks.TaskCanceledException: A task was canceled.
       at EasyNetQ.Persistent.PersistentChannel.InvokeChannelActionAsync
       at EasyNetQ.DefaultPubSub.PublishAsync
       at ChannelService.Service.ChannelAppService.CreateAsync ... line 74

The caller is told the operation failed, the data exists, and no other service was told about it. A caller that retries gets `409 Conflict`, because the name is taken. This is a write to two places with no way to undo either.

### 2. A failing message handler loses the message

`MessagePostedHandler` writes to the database and then publishes `ChannelActivityUpdatedEvent`. If either throws, EasyNetQ's default error handling moves the message to an error queue. Nothing retries it, so a brief database or broker problem loses that channel's activity update permanently.

### 3. The handler writes to two places as well

Same shape as finding 1, one layer down: the database is updated first, the event published second. If the publish fails, the channel's `LastActivityAt` has moved but no other service knows.

## Assuming a stable network

### 4. No timeout belongs to this service

The 11 seconds in finding 1 are EasyNetQ's own limit, not a value this service chose. Nothing in the code says how long a publish may take, so the figure can change with a library upgrade or a connection-string setting without anyone noticing.

Eleven seconds is far too long to sit inside an HTTP request that normally answers in milliseconds.

### 5. Nothing is retried — and the broker comes back quickly — measured

Restarting RabbitMQ while the service kept running: a `POST /channels` sent during the restart failed as in finding 1, and a second one a minute later succeeded with no intervention.

The interruption was short and transient. A single retry after a few seconds would have saved the lost event. There is no retry anywhere in the service.

## Data stores

### 6. A real database would have no timeout or retry either

Entity Framework Core runs against an in-memory store today, so queries cannot hang. Against a real database, every endpoint would fail outright on an outage, and nothing in the code limits how long a query may take.

### 7. `GetForUserAsync` loads everything before filtering

`ChannelAppService.GetForUserAsync` calls `GetAllAsync`, which loads every channel with all of its members, and filters in memory afterwards. The work grows with the size of the database rather than with the size of the answer.

A slow response is worse than a failed one: the request holds a thread while it waits, and under load that is how a service runs out of capacity.

## Inbound traffic

### 8. Nothing limits how many requests arrive

There is no rate limiting. A burst — intended or not — reaches the service at full speed, and findings 4 and 7 make each request potentially expensive.

### 9. Nothing reports whether the service is healthy

`docker-compose.yml` has a health check for RabbitMQ but none for the service itself, and the service exposes no health endpoint. If it lost its broker connection or its database, nothing outside it would notice.

## Tested and found not to be a problem

**The subscription survives a broker restart.** `MessageHandlerBackgroundService` subscribes once at startup, which raised the question of whether the service recovers if the broker restarts later.

It does. After `docker restart bizcord-rabbitmq`, a channel created a minute later still reached `ChannelCreatedHandler`. EasyNetQ re-establishes both the connection and the subscriptions on its own.

The gap is not the subscription but the window during the outage, which finding 5 covers.

## Measures implemented

Findings 1, 4 and 5 were addressed together, since all three concern the single publish inside `POST /channels`.

### Which layer

Timeout and retry live in `EasyNetQMessageClient`, the adapter that wraps EasyNetQ. How long a publish may take, and how often it is worth trying again, are properties of the transport; no caller should have to know them.

The fallback lives in `ChannelAppService`. Whether a channel may be created without anyone being told is a question about the business, not about RabbitMQ, and the service layer is the first place that can answer it.

### Which patterns, and why

**Timeout**, because the 11 seconds were EasyNetQ's own and nothing in this service had chosen them. One second per attempt: a publish to a healthy broker completes in well under a millisecond.

**Retry**, because the outage measured in finding 5 lasted under a minute and healed by itself. Two retries, 200 ms and 400 ms apart.

**Fallback**, because the alternative is finding 1: the caller is told the operation failed while the data is already stored, and a retry then collides with the name that is now taken. Implemented as a `try`/`catch` rather than Polly's fallback strategy, which supplies a replacement value and so does not apply to an operation that returns none.

**Not a circuit breaker.** Three attempts over roughly four seconds is a cost this service can carry, and the outage that was measured was short. A breaker earns its place when outages are long and traffic is high; neither has been observed here.

### How it was tested

One row per measure. "How" is the way it was exercised, "Result" what came out.

| Measure | How | Result |
| --- | --- | --- |
| Fallback | Unit test with a message client that always throws | The channel is returned and stored, and publishing was attempted |
| Timeout and retry | Broker stopped, `POST /channels` timed | 4.4 s and `201`, against 11 s and `500` before the change |
| Normal operation | Broker running, `POST /channels` timed | 0.14 s, and the event reached its handler |

The log during the outage shows both the retries and the fallback:

    Publish failed, retrying in 00:00:00.2000000. Attempt 2 of 3.
    Publish failed, retrying in 00:00:00.4000000. Attempt 3 of 3.
    Channel 80ec8617-... was created but ChannelCreated could not be published

### What is still wrong

The event is still lost. The caller no longer sees a failure, and the loss is recorded rather than silent, but no other service learns about the channel.

The fix is an outbox: store the event in the database in the same operation as the channel, and let a background process publish it once the broker is reachable. That removes the write to two places instead of softening its consequences, and it is the right next step.

Findings 2, 3, 6, 7, 8 and 9 are untouched.