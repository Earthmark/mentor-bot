# Technical Dependencies

[Root Doc (Start Here)](README.md)

The services the bot depends on, and the mechanisms that are expected to work.

## 1. Resonite API

1. Setting the bot-group cloud variable (in-resonite [mentor](MentorFlows.md) actions)
2. OAuth login of users ([login](LoginFlow.md))
3. Getting the user ID of an oauth logged in user ([login](LoginFlow.md))

## 2. Resonite Client

1. Making a Websocket request to the mentor api (in-resonite [mentor](MentorFlows.md) and [user](UserFlows.md) actions)
2. Reading a cloud variable from the Resonite API (in-resonite [mentor](MentorFlows.md) actions)
3. Making a POST request to the mentor api (in-resonite [mentor](MentorFlows.md) and [user](UserFlows.md) actions)

## Other

1. A database, probably postgres or mysql.
2. A web browser.
3. A hosting environment able to run dotnet.
