using System.Net;
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

        return result ?? new RegisterViewModel([], [], []);
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

    public async Task<ConfirmFitResponseModel> ConfirmFitAsync(
        Guid id,
        string? additionalInfo,
        string? siteCode,
        CancellationToken cancellationToken = default)
    {
        var payload = new { additionalInfo, siteCode };
        var response = await _httpClient.PostAsJsonAsync(
            $"api/v1/register/{id}/confirm-fit", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ConfirmFitResponseModel>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialise confirm-fit response.");
    }

    public async Task<DeclareNotFitResponseModel> DeclareNotFitAsync(
        Guid id,
        string? additionalInfo,
        CancellationToken cancellationToken = default)
    {
        var payload = new { additionalInfo };
        var response = await _httpClient.PostAsJsonAsync(
            $"api/v1/register/{id}/declare-not-fit", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DeclareNotFitResponseModel>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialise declare-not-fit response.");
    }

    public async Task<SiteCodeSubmissionResult> SubmitSiteCodeAsync(
        Guid id,
        string siteCode,
        string? additionalInfo,
        CancellationToken cancellationToken = default)
    {
        var payload = new { siteCode, additionalInfo };
        var response = await _httpClient.PostAsJsonAsync(
            $"api/v1/register/{id}/submit-site-code", payload, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new SiteCodeSubmissionResult(
                false,
                null,
                "That site code is not recognised. Check with the Site Controller and try again.");
        }

        response.EnsureSuccessStatusCode();

        var confirmation = await response.Content.ReadFromJsonAsync<ConfirmFitResponseModel>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialise submit-site-code response.");

        return new SiteCodeSubmissionResult(true, confirmation, null);
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
