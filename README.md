# What is LucasCanavan.MicrosoftAccountAuthenticationClient

The LucasCanavan.MicrosoftAccountAuthenticationClient project is an alternative Microsoft Account authentication client for ASP.NET MVC4 / DotNetOpenAuth.  The default authentication client is hard-coded to the "wl.basic" scope and doesn't catch all of the useful return values (eg authentication_token).  This project allows you to specify the scope(s) and capture all of the protentially useful data returned upon a successful authentication.  

# Getting Started 

To use LucasCanavan.MicrosoftAccountAuthenticationClient, all you need to do is replace the following code:

App_Start\AuthConfig.cs

```C#
OAuthWebSecurity.RegisterMicrosoftClient(
	clientId: "YOUR_CLIENT_ID",
	clientSecret: "YOUR_CLIENT_SECRET");
```
With the following code:

```C#

OAuthWebSecurity.RegisterClient(new MicrosoftAccountAuthenticationClient(
    clientId: ConfigurationManager.AppSettings["MicrosoftAccountClientId"],
    clientSecret: ConfigurationManager.AppSettings["MicrosoftAccountClientSecret"],
    scopes: ConfigurationManager.AppSettings["MicrosoftAccountScopes"]), "Microsoft", null);
```
## Configuration

Whilst it's possible to hard-code your configuration settings passed to RegisterClient(), I prefer to read them from the web.config file as illustrated above and below.  Note, you can specific multiple scopes simply by providing a comma-seperated list of the scopes you desire.

```xml
<configuration>
...
  <appSettings>
    ...
    <add key="MicrosoftAccountClientId" value="YOUR_CLIENT_ID" />
    <add key="MicrosoftAccountClientSecret" value="YOUR_CLIENT_SECRET" />
    <add key="MicrosoftAccountDefaultRedirectUrl" value="YOUR_DOMAIN" />
    <add key="MicrosoftAccountScopes" value="wl.signin,wl.emails" />
  </appSettings>
```