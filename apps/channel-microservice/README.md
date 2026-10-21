# Channel Service

Part of the Bizcord platform. Owns channels and channel membership,
and is the source of truth for who may read and write in a channel.

## Running with Docker Compose

Starts RabbitMQ and the service together. The service waits for
RabbitMQ to pass its health check before starting.

    docker compose up -d --build

- API: http://localhost:8000
- RabbitMQ management UI: http://localhost:15672 (guest / guest)

Stop everything again:

    docker compose down

## Running the tests

Unit tests run on their own. The integration test needs a running broker:

    docker compose up -d rabbitmq
    dotnet test

## Running locally

Requires RabbitMQ on localhost:5672.

    dotnet run --project src/ChannelService

## Endpoints

One row per endpoint. "Method" is the HTTP method, "Path" the address, and "Purpose" what the call does.

| Method | Path              | Purpose                                 |
| ------ | ----------------- | --------------------------------------- |
| GET    | /channels         | List all channels                       |
| GET    | /channels/{id}    | Get a single channel                    |
| GET    | /channels/user/me | List channels the caller is a member of |
| POST   | /channels         | Create a channel                        |
| PUT    | /channels/{id}    | Update a channel                        |
| DELETE | /channels/{id}    | Delete a channel                        |

Example:

    curl -X POST http://localhost:8000/channels \
      -H "Content-Type: application/json" \
      -d '{ "name": "general", "description": "General discussion" }'

### Caller identity

`GET /channels/user/me` needs an `X-UserId` header holding the caller's id, and returns `401` without it. `POST /channels` reads the same header and records the caller as an `Admin` member of the new channel; without the header the channel is still created, but with no members.

An API gateway in front of this service is expected to populate that header from a validated token and to strip whatever the client sent. A direct call to this service bypasses that, so a real deployment must keep the service unreachable except through the gateway.

## Structure

    src/ChannelService/         service code
    src/ChannelService.Shared/  this service's outward contract (ChannelDto)
    src/Shared.Contracts/       events shared across the system
    tests/                      unit, contract and integration tests