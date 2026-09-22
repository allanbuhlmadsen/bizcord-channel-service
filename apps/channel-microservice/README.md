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

## Running locally

Requires RabbitMQ on localhost:5672.

    dotnet run --project src/ChannelService

## Endpoints

| Method | Path            | Purpose                |
| ------ | --------------- | ---------------------- |
| GET    | /channels       | List all channels      |
| GET    | /channels/{id}  | Get a single channel   |
| POST   | /channels       | Create a channel       |
| PUT    | /channels/{id}  | Update a channel       |
| DELETE | /channels/{id}  | Delete a channel       |

Example:

    curl -X POST http://localhost:8000/channels \
      -H "Content-Type: application/json" \
      -d '{ "name": "general", "description": "General discussion" }'

## Structure

    src/ChannelService/         service code
    src/ChannelService.Shared/  contract other services can reference
    tests/                      unit tests