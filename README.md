# acs-callautomation-devx

An exploration to use an async/await pattern for Azure Communication Services CallAutomation flows.

## Limitation

This async/await pattern creates a sticky client-server relation. If your service runs behind a load balancer and gets scaled out there is no guarantee that the same server instance receives the event to continue. In this case you need to encode the server instance in the callback URL and use a routing mechanism to direct to the right server.

## Prerequisites
- An Azure Communication Services resource with a purchased phone number and created Communication identity

1 Run `ngrok http https://localhost:7072` from a console, and note the ngrok url
2 Fill out the appsettings.json with your Communication resource connection string, the owned phone number, the ngrok url as BaseUrl, and the communication identity
3 Build and run the solution
4 Use the swagger UI with "Try it" and post against the /current/run route with your (US or Canada) phone number in E.164 format or against the /eventAwaiter/run route.