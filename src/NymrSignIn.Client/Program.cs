using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using NymrSignIn.Client;
using NymrSignIn.Client.Admin.Services;
using NymrSignIn.Client.Register.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl")
    ?? builder.HostEnvironment.BaseAddress;

// Kiosk HttpClient — anonymous, unchanged
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<IRegisterApiService, RegisterApiService>();
builder.Services.AddScoped<SignInFlowState>();

// Admin auth — MSAL via Azure AD
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    var apiScope = builder.Configuration["AzureAd:ApiScope"];
    if (!string.IsNullOrWhiteSpace(apiScope))
    {
        options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
    }
});

// Named HttpClient for authenticated admin calls — custom handler attaches bearer tokens
// for calls to the API origin (different from the Blazor app origin).
builder.Services.AddScoped<ApiAuthorizationMessageHandler>();
builder.Services
    .AddHttpClient("AdminApi", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

builder.Services.AddScoped<IAdminApiService, AdminApiService>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
