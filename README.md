# mentor-bot
A bot for messaging mentors that a new user calls for aid.

## Architecture
The bot is two main services, the maintainer and the http server. Currently they are bundled together (previously it was two services, and it may be multiple services in the future).

All state is stored in the discord channel itself, each message is a ticket and that message is updated with the current state of the ticket.

### Maintainer
This handles reactions from Discord and is responsible for:

* Advance the Ticket state machine
* Notify the http server of advances to the state machine

### Http Server
This handles websocket connections and is responsible for:

* Reporting the ticket status to a client
* Processing a cancel request from the client
* Server health checks (/health)
* (stretch) reporting that a client is still connected
* (stretch) Provide the ability for a mentor to claim tickets from inside Neos (dashboard facet)
* (stretch) Provide real time comms, possibly help desk style with onsite as last resort.

### (stretch) Message Bus
With how the server works it's not viable to split the server into two parts (even though it makes more sense to do that). To split the servers a message bus is the most likely solution, as it can notify the other servers of ticket updates (this turns the server into a stereotypical real-time chat app, the stereotypical usage of a message bus and websocket).

At that point it may not even make sense for the discord reaction integration, instead relying on in-game assistance.

## Ticket States
These are the different states a ticket can go through, and the conditions where a ticket advances.

### Requested
The ticket is unclaimed, and is waiting for a mentor to claim it.

* A mentor can advance the ticket to responding.
* The mentee can advance the ticket to canceled.

### Responding
A mentor is currently on route or is on site for the ticket.

* A mentor can advance the ticket to completed.
* The mentee can advance the ticket to canceled.

### Completed
The mentor has marked the ticket as completed.

### Canceled
The mentee has canceled the ticket on their end.
