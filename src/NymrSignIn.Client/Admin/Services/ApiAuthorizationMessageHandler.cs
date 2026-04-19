using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;

namespace NymrSignIn.Client.Admin.Services;

public sealed class ApiAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public ApiAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IConfiguration configuration)
        : base(provider, navigation)
    {
        var apiBaseUrl = configuration["ApiBaseUrl"]
            ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");
        var apiScope = configuration["AzureAd:ApiScope"]
            ?? throw new InvalidOperationException("AzureAd:ApiScope is not configured.");

        ConfigureHandler(
            authorizedUrls: [apiBaseUrl],
            scopes: [apiScope]);
    }
}
