using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace OneginiAuthClient.Onegini.AspNetCore.Authentication.OneginiOIDC
{
    public class OneginiOpenIdConnectHandler : OpenIdConnectHandler
    {
        public OneginiOpenIdConnectHandler(IOptionsMonitor<OpenIdConnectOptions> options, ILoggerFactory logger, HtmlEncoder htmlEncoder, UrlEncoder encoder, ISystemClock clock) : base(options, logger, htmlEncoder, encoder, clock)
        {
        }

        protected override async Task<OpenIdConnectMessage> RedeemAuthorizationCodeAsync(OpenIdConnectMessage tokenEndpointRequest)
        {
            //Logger.RedeemingCodeForTokens(); //Can not access due to protection level so just let it be

            OpenIdConnectHandler idConnectHandler = this;
            OpenIdConnectConfiguration configurationAsync = await idConnectHandler.Options.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
            
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, configurationAsync.TokenEndpoint);
            
            //Add header instead of body -> https://tools.ietf.org/html/rfc6749#section-2.3.2
            var basicAuthHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Options.ClientId}:{Options.ClientSecret}"));
            requestMessage.Headers.Add("Authorization", basicAuthHeader);

            var parameters = tokenEndpointRequest.Parameters;
            parameters.Remove("client_id");
            parameters.Remove("client_secret");
            
            requestMessage.Content = new FormUrlEncodedContent(parameters);

            var responseMessage = await Backchannel.SendAsync(requestMessage);

            var contentMediaType = responseMessage.Content.Headers.ContentType?.MediaType;
            if (string.IsNullOrEmpty(contentMediaType))
            {
                Logger.LogDebug($"Unexpected token response format. Status Code: {(int)responseMessage.StatusCode}. Content-Type header is missing.");
            }
            else if (!string.Equals(contentMediaType, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogDebug($"Unexpected token response format. Status Code: {(int)responseMessage.StatusCode}. Content-Type {responseMessage.Content.Headers.ContentType}.");
            }

            // Error handling:
            // 1. If the response body can't be parsed as json, throws.
            // 2. If the response's status code is not in 2XX range, throw OpenIdConnectProtocolException. If the body is correct parsed,
            //    pass the error information from body to the exception.
            OpenIdConnectMessage message;
            try
            {
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                message = new OpenIdConnectMessage(responseContent);
            }
            catch (Exception ex)
            {
                throw new OpenIdConnectProtocolException($"Failed to parse token response body as JSON. Status Code: {(int)responseMessage.StatusCode}. Content-Type: {responseMessage.Content.Headers.ContentType}", ex);
            }

            if (!responseMessage.IsSuccessStatusCode)
            {
                string errorDescription = message.ErrorDescription ?? "error_description is null";
                string errorUri = message.ErrorUri ?? "error_uri is null";
                
                throw new OpenIdConnectProtocolException(string.Format(CultureInfo.InvariantCulture, "", message.Error, errorDescription, errorUri));
            }

            return message;
        }
    }
}