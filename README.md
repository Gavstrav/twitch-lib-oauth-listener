# twitch-lib-oauth-listener
A simple C# TwitchLib OAuth authentication helper that launches a browser-based Twitch login flow and captures the resulting access token using a local HTTP listener.

## Notes

- Requires TwitchLib.Client and TwitchLib.Api NuGet packages https://github.com/TwitchLib/TwitchLib
- Uses OAuth Implicit Flow https://dev.twitch.tv/docs/authentication/getting-tokens-oauth#implicit-grant-flow

## Setup

To use `TwitchOAuthListener`, you need a Twitch application Client ID.

### 1. Create a Twitch Application
Go to:
https://dev.twitch.tv/console/apps

Click **Register Your Application** and set:

- **OAuth Redirect URL:** `http://localhost:3000`

After creation, copy your **Client ID**.

---

### 2. Use the Client ID in your app

```csharp
var auth = new TwitchAuthListener("YOUR_CLIENT_ID");
auth.Start();
