namespace NymrSignIn.Client.Register.Models;

public sealed class SignInFormModel
{
    public string Name { get; set; } = string.Empty;
    public string Organisation { get; set; } = string.Empty;
    public string? SignatureBase64 { get; set; }
}
