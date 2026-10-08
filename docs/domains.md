# Domains and microservices

Each service below owns one functional domain. The split follows what each service owns rather than what it does technically: no two services are the source of truth for the same data.

## Auth Service

Handles sign-in and sign-out, and issues the token other services use to identify the caller. Owns credentials and sessions, and nothing else about the user.

## User Profile Service

Owns the user's display name, avatar and status text. Other services look profiles up here instead of keeping their own copy of user data.

## Channel Service

Owns channels and channel membership, and is the single source of truth for who is allowed to read and write in a given channel. Implemented in this repository.

## Messaging Service

Owns messages and delivers them in real time to connected clients. Asks the Channel Service whether the sender is a member before accepting a message.

## Reaction Service

Owns reactions attached to messages. Kept apart from the Messaging Service so that a large volume of reactions does not compete with message delivery.

## Notification Service

Reacts to events from the other services and delivers notifications to users who are not currently connected. Owns notification settings and delivery history.

## Out of scope

File attachments, search and moderation are left out of this design. They are likely candidates for further services once the requirements are clearer.