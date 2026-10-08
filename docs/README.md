# Bizcord — Analysis & Design

Analysis and design documentation for the Bizcord platform, written from the perspective of the Channel Service.

Bizcord is a business communication platform similar to Discord or Slack. The system is split into microservices, each owned by one team. This repository contains the Channel Service.

## Contents

- [Domains and microservices](domains.md) — the functional domains identified in the system, and what each service is responsible for
- [Boundaries and data ownership](boundaries.md) — where the lines between services are drawn, and who owns which data
- [Interactions](interactions.md) — how the services communicate, and the event flows this service takes part in

## Scope

The system-wide view is a design proposal: only the Channel Service exists in this repository. Services other than Channel Service are described as they are expected to behave, not as implemented systems.

The Channel Service sections describe what is actually implemented. Where the implementation differs from the original workshop design, the current state is documented and the change noted.