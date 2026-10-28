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