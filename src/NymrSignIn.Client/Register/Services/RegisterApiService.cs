using System.Net.Http.Json;
using NymrSignIn.Client.Register.Models;

namespace NymrSignIn.Client.Register.Services;

public sealed class RegisterApiService : IRegisterApiService
{
    private readonly HttpClient _httpClient;

    public RegisterApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RegisterViewModel> GetTodaysRegisterAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<RegisterViewModel>(
            "api/v1/register", cancellationToken);

        return result ?? new RegisterViewModel([], []);
    }

    public async Task<SignInResponseModel> SignInAsync(SignInFormModel model, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            name = model.Name,
            organisation = model.Organisation,
            signatureBase64 = model.SignatureBase64
        };

        var response = await _httpClient.PostAsJsonAsync("api/v1/register/signin", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SignInResponseModel>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialise sign-in response.");
    }

    public async Task<SignOutResponseModel> SignOutAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/v1/register/{id}/signout", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SignOutResponseModel>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialise sign-out response.");
    }

    public async Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetByteArrayAsync("api/v1/register/export", cancellationToken);
    }

    public async Task SendDailyEmailAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("api/v1/register/email", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
