using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using NymrSignIn.Application.Register;

namespace NymrSignIn.Infrastructure.Register;

public sealed class SiteCodeGenerator : ISiteCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int BodyLength = 4;

    private readonly SiteOptions _options;

    public SiteCodeGenerator(IOptions<SiteOptions> options)
    {
        _options = options.Value;
    }

    public string Generate()
    {
        Span<char> body = stackalloc char[BodyLength];
        for (var i = 0; i < BodyLength; i++)
        {
            body[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        var prefix = string.IsNullOrWhiteSpace(_options.SiteCodePrefix)
            ? "SITE"
            : _options.SiteCodePrefix.Trim().ToUpperInvariant();

        return $"{prefix}-{new string(body)}";
    }
}
