# Mentor Flows

[Root Doc (Start Here)](README.md)

Users do not need to log in to use the API.

## Creating a Ticket (Resonite UI)

The user is prompted to create a ticket using a UI wizard in-engine.

The Resonite client makes a websocket connection to the mentor bot, which sends back ticket updates.

In practice once the client gets the first update (the one saying the ticket was created) the websocket is closed, and the route with a ticket_id is used instead. This was easier to implement in resonite, although it is not good practice.

There is a rate limit on the ticket create call, and additional protections may be needed in the future (such as including the user ID).

```mermaid
sequenceDiagram
    actor User
    participant Resonite
    participant MentorBot
    actor Mentor
    User->>Resonite: Get Help
    Resonite->>MentorBot: WS /api/mentee?userId=_&lang=_&session=_&...
    Resonite->>MentorBot: WS /api/mentee/{ticket_id}
    alt User cancels ticket 
        User->>Resonite: Close Ticket
        Resonite->>MentorBot: urlencoded WS message with "type=cancel"
        MentorBot-->>Resonite: Close Websocket
    else Mentor responds to ticket
        Mentor->>MentorBot: Claim Ticket
        MentorBot->>Resonite: Ticket Claimed (WS message)
        Resonite->>User: Shown Mentor's contact info
        Mentor->>Resonite: Connects to User's session
        Mentor->>MentorBot: Resolve Ticket
        MentorBot-->>Resonite: Close Websocket
    end
```

