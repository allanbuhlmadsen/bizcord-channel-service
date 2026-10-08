# Boundaries and data ownership

## The rule

Each service owns its own data. No service reads or writes another service's database directly. If a service needs data it does not own, it asks the owner over the network or reacts to an event the owner publishes.

## Who owns what

The table lists one row per service. "Owns" is the data the service is the single source of truth for. "Does not own" names data the service works with but must get from elsewhere — it is there to make the boundary visible, not to list everything a service lacks.

| Service | Owns | Does not own |
| --- | --- | --- |
| Auth | Credentials, sessions | Display names, channel membership |
| User Profile | Display name, avatar, status text | Credentials, channel membership |
| Channel | Channels, channel membership, roles | User identity, messages |
| Messaging | Messages | Channel membership, user identity |
| Reaction | Reactions | Messages, user identity |
| Notification | Notification settings, delivery history | The events it reacts to |

## Channel Service in detail

### What it owns

**Channels.** A channel has an identity, a name, a description, a creation timestamp, and a last-activity timestamp. The name is a value object that cannot exist in an invalid state: it must be non-empty and at most 80 characters.

**Channel membership.** A membership links a user to a channel, with a role and a join timestamp. The roles are `Member`, `Moderator` and `Admin`.

Membership can only be created through the channel itself, which enforces that the same user cannot join the same channel twice.

### What it deliberately does not own

**Users.** A membership stores only the user's identity as a value. Display name, avatar and status belong to the User Profile Service and are never copied here.

**Messages.** The Channel Service knows when a message was posted, because it reacts to an event, but it stores no message content.

### Internal model versus shared model

The domain model is this service's internal representation and is never exposed. A separate shared model carries what other services may see:

| Field | In shared model | Reason |
| --- | --- | --- |
| Id | Yes | Needed to reference the channel |
| Name | Yes, as plain text | Other services should not need to know this service's naming rules |
| Description | Yes | Useful to display |
| CreatedAt | Yes | Useful for ordering and display |
| MemberCount | Yes | Enough to act on, without exposing who the members are |
| The member list | No | Roles and join times are internal |
| LastActivityAt | No | Published as an event instead |

The second table reads one row per field of the channel as other services see it. "In shared model" says whether the field crosses the boundary, and "Reason" says why.