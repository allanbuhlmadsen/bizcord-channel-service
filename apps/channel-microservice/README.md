# Channel Service

Part of the Bizcord platform. Owns channels and channel membership,
and is the source of truth for who may read and write in a channel.

## Running with Docker Compose

Starts RabbitMQ, HashiCorp Vault and the service together. The service waits for RabbitMQ to pass its health check, and reads its messaging credentials from Vault at startup. See "Secrets" below for what Vault needs before the first run.

    docker compose up -d --build

- API: http://localhost:8000
- RabbitMQ management UI: http://localhost:15672 (guest / guest)
- Vault UI: http://localhost:8200

Stop everything again:

    docker compose down

Do not use `docker compose down -v`. That deletes the `vault-data` volume, and with it every secret and the initialisation itself.

## Secrets

The RabbitMQ username and password are not in this repository. They live in Vault under `secret/channel-service`, and the service reads them at startup. The host name stays in ordinary configuration — `localhost` in `appsettings.json`, `rabbitmq` in `docker-compose.yml` — because a host name is not a secret.

### First-time setup

Vault starts empty and sealed.

1. Open http://localhost:8200 and initialise it with 3 key shares and a threshold of 2. Save the unseal keys and the root token somewhere outside this repository. They are shown once.
2. Unseal with two of the keys and sign in with the root token.
3. Create the secret, a read-only policy and a scoped token:

       docker compose exec vault sh
       vault login
       vault secrets enable -path=secret kv-v2
       vault kv put secret/channel-service Username=guest Password=guest
       printf 'path "secret/data/channel-service" { capabilities = ["read"] }' | vault policy write channel-service-read -
       vault token create -policy=channel-service-read -no-default-policy -ttl=1h
       exit

4. Put the token from the last command in a file named `.env` next to `docker-compose.yml`:

       VAULT_TOKEN=hvs.your-token-here

   `.env` is ignored by git and must stay that way. The root token is for administration and must never be given to the service.

### Every time

Vault is sealed again whenever its container restarts. Unseal it at http://localhost:8200 before starting the service.

The application token expires. Create a new one with the `vault token create` command above and update `.env`.

### When Vault is unavailable

The service does not start. There is no fallback, and that is deliberate: a fallback would mean a copy of the credentials somewhere outside Vault, which is what this is meant to remove.

A service that is **already running** keeps working while Vault is sealed, because it read the secret once at startup and holds it in memory. The task text says requests should fail while Vault is sealed; that holds for a service that reads the secret on every request, not for one that reads it at startup, which is what the same task text asks for. The failure shows up on the next start.

## Running the tests

Unit tests run on their own. The integration test boots the service through `WebApplicationFactory`, so it needs everything the service needs: a running broker, an unsealed Vault, and a token. In PowerShell:

    docker compose up -d rabbitmq vault
    $env:VAULT_TOKEN = "hvs.your-token-here"
    dotnet test

Stop the `channel-service` container first if it is running. It shares the subscription id with the test and will consume the test's messages.

## Running locally

Requires RabbitMQ on localhost:5672 and an unsealed Vault on localhost:8200. In PowerShell:

    $env:VAULT_TOKEN = "hvs.your-token-here"
    dotnet run --project src/ChannelService

## Endpoints

One row per endpoint. "Method" is the HTTP method, "Path" the address, and "Purpose" what the call does.

| Method | Path                            | Purpose                                                      |
| ------ | ------------------------------- | ------------------------------------------------------------ |
| GET    | /channels                       | List all channels                                            |
| GET    | /channels/{id}                  | Get a single channel                                         |
| GET    | /channels/user/me               | List channels the caller is a member of                      |
| GET    | /channels/{id}/members/{userId} | Whether a user is a member of a channel, and with which role |
| POST   | /channels                       | Create a channel                                             |
| PUT    | /channels/{id}                  | Update a channel                                             |
| DELETE | /channels/{id}                  | Delete a channel                                             |

Example:

    curl -X POST http://localhost:8000/channels \
      -H "Content-Type: application/json" \
      -d '{ "name": "general", "description": "General discussion" }'

### Caller identity

`GET /channels/user/me` needs an `X-UserId` header holding the caller's id, and returns `401` without it. `POST /channels` reads the same header and records the caller as an `Admin` member of the new channel; without the header the channel is still created, but with no members.

An API gateway in front of this service is expected to populate that header from a validated token and to strip whatever the client sent. A direct call to this service bypasses that, so a real deployment must keep the service unreachable except through the gateway.

### Internal endpoints

`GET /channels/{id}/members/{userId}` is meant for other services, not for clients. It needs an `X-Role` header with the value `internal-service` and returns `403` without one.

An API gateway sets that header from a `role` claim in the caller's token and removes whatever the client sent, in the same way it sets `X-UserId`. A client's own token is refused even though it is valid, because it carries a scope rather than a role.

## Structure

    src/ChannelService/         service code
    src/ChannelService.Shared/  this service's outward contract (DTOs)
    src/Shared.Contracts/       events shared across the system
    tests/                      unit, contract and integration tests