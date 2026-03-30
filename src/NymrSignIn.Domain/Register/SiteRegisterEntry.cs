namespace NymrSignIn.Domain.Register;

public class SiteRegisterEntry
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Organisation { get; private set; } = string.Empty;
    public string? SignatureUrl { get; private set; }
    public DateOnly DateIn { get; private set; }
    public TimeOnly TimeIn { get; private set; }
    public TimeOnly? TimeOut { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsSignedOut => TimeOut.HasValue;

    private SiteRegisterEntry() { }

    public static SiteRegisterEntry Create(
        string name,
        string organisation,
        DateOnly dateIn,
        TimeOnly timeIn,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(organisation))
            throw new ArgumentException("Organisation is required.", nameof(organisation));

        return new SiteRegisterEntry
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Organisation = organisation.Trim(),
            DateIn = dateIn,
            TimeIn = timeIn,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void SignOut(TimeOnly timeOut)
    {
        if (IsSignedOut)
            throw new InvalidOperationException($"Entry {Id} is already signed out.");

        TimeOut = timeOut;
    }

    public void SetSignatureUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Signature URL cannot be empty.", nameof(url));

        SignatureUrl = url;
    }
}
