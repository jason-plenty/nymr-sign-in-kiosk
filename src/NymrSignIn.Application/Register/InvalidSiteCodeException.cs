namespace NymrSignIn.Application.Register;

public sealed class InvalidSiteCodeException : Exception
{
    public InvalidSiteCodeException(string message) : base(message) { }
}
