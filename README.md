# OpenID Connect Relying Party Example for ASP.NET.
This example covers how to implement and configure a ASP.NET Core project to work OpenID Connect (OIDC) utlizing 
Onegini's OpenID Connect Provider (OP).

## Clone and configure your IDE
To get the example up and running, clone the project and setup the configuration:

Clone this repo using https or ssh. e.g.:

`git clone https://github.com/Onegini/dotnet-mvc-oidc-example.git`

### Rider and Visual Studio

Go to `File->Open->Solution or Project` and open dotnet-mvc-oidc-example.sln. 

Run configuration can be changed within
[launchSettings.json](/DotnetAspCoreMvcExample/Properties/launchSettings.json).

## Onegini Configuration
You'll need to properly setup your client using the Onegini Admin panel before you can begin testing.
Refer to the [OpenID Connect documentation](https://docs.onegini.com/msp/6.0/token-server/topics/oidc/index.html). 

This project requires a Web client that accepts the `openid` and `profile` scope. Add the scope `email` to see additional claims when the Identity Provider returns this information.

The Onegini Token Server only redirects to preconfigured endpoints after login or logout. You must configure the following endpoints in the Onegini Token Server:
    * Redirect URL: `https://localhost:5001/signin-external`
    * Post Logout Redirect URL: `https://localhost:5001/signout-callback-oidc`

## Set up the application configuration
We have provided some sample configuration for the project below, it uses the default appsettings.json setup. If you 
wish to use different configuration, you may need to modify the code inside
['Startup.cs'](/DotnetAspCoreMvcExample/Startup.cs) accordingly. 

We have provided two application settings json files for you with placeholders, one for development and one for
production. See [appsettings.Development.json](/DotnetAspCoreMvcExample/appsettings.Development.json) and 
[appsettings.json](/DotnetAspCoreMvcExample/appsettings.json). You'll need to fill in real values for the placeholders. 
Here are some examples of what that might look like:

_appsettings.Development.json_

    {
      "Logging": {
        "LogLevel": {
          "Default": "Warning"
        }
      },
      "AllowedHosts": "*",
      "oidc":{
        "issuer":"http://localhost:7878/oauth/",
        "clientId": "openid-client",
        "clientSecret": "secret",
        "requireHttpsMetadata": false
      }
    }
    
## Run and test
Run and test the example. You should be able to see a page with login button at the upper right. When you click it
you're redirected to the OP to login. When successful, you should be redirected back. Now you should see a logout button
and a username. The username will be whatever is returned as the "sub" claim. The "sub" is always returned as part of the [standard claims](http://openid.net/specs/openid-connect-core-1_0.html#StandardClaims)).

## Components in this application

### Configuration
The configuration we set up is used inside `Startup`. You'll need to register your application to get a `clientId`
and a `clientSecret`. Also you need to know the `issuer` url where the OP resides. Refer to the 
[Onegini Configuration](#onegini-configuration) section above if you don't know where to get these values from or ask
your administrator.

### Startup
Inside the `ConfigureServices(IServiceCollection services)` method we use the `AddOneginiOpenIdConnect` builder method 
to add a scheme that will authenticate using our OP. Inside the callback of this builder, we can configure the OIDC
settings we want to use. One important thing to review is the `NameClaimType` as shown in this code:

    o.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = ClaimTypes.NameIdentifier
    };
    
It dictates to the ASP.NET framework that we want to use that claim as the username. You can change it depending on the
claims available in your environment. Default claims within ASP.NET will have the name provided by the `ClaimTypes` class.
Custom claims will have their original name, and you can refer to them using their original name.

    o.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "recovery_email" //Or whatever name claim name you want to use
    };
    
Inside the `Configure(IApplicationBuilder app, IHostingEnvironment env)` method we call `app.UseAuthentication();`. This
will trigger the framework to use the authentication middleware setup inside `ConfigureServices`.

### Onegini OIDC handler
In the above chapter, you might have noticed that we use the `AddOneginiOpenIdConnect` builder method. This method does 
not exist in the default ASP.NET Core library. ASP offers the `AddOpenIdConnect` method, but due to an issue with [RFC6749 2.3.1.](https://tools.ietf.org/html/rfc6749#section-2.3.1)
in Microsoft's implementation of the OAuth spec, it does not work with our OP. To fix this issue we extend the original
OIDC handler and overwrite the `RedeemAuthorizationCodeAsync` method. This is done inside the `OneginiOpenIdConnectHandler`.
 
The `OneginiOpenIdConnectExtensions` class adds the builder methods to the Authentication builder. To add the Onegini 
OIDC handler you can create a class library or you can add it directly to your project.

### Controller
For the `OneginiOpenIdConnectHandler` to work, we need to add an AccountController. The `Login()` and `Logout()` are 
required, the `Profile` method is mainly for demonstration purpose. It shows you how to retrieve the access token used to
query any backchannel resource gateways. Also it loads the Claims page that will show you the current set of claims 
retrieved from the OP during authentication.

When you want a user to be authenticated or authorized you can use the ASP.NET default patterns and annotations like `[Authorize]`.

### View
As stated above there is a ['Claims.cshtml'](/DotnetAspCoreMvcExample/Views/Account/Claims.cshtml) file that shows the claims for demonstration 
purpose. You can see how we get access to the claims within the view, you can use this to change the view based on the 
claims available.

Inside [_LoginPartial.cshtml](/DotnetAspCoreMvcExample/Views/Shared/_LoginPartial.cshtml), you can find the logic we use
to show the login url when a user is not authenticated and a logout url when the user is authenticated.