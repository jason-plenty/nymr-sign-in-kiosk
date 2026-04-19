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

    public SiteStatus Status { get; private set; }
    public MedicalStatus MedicalStatus { get; private set; }
    public string? AdditionalInfo { get; private set; }
    public string? SiteCode { get; private set; }
    public string? SiteCodeGenerated { get; private set; }

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
            CreatedAtUtc = createdAtUtc,
            Status = SiteStatus.Pending,
            MedicalStatus = MedicalStatus.NotDeclared
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

    public void DeclareFit(string? additionalInfo, string? siteCode)
    {
        if (Status is not SiteStatus.Pending)
            throw new InvalidOperationException($"Entry {Id} is in status {Status}; cannot declare fit.");

        Status = SiteStatus.OnSite;
        MedicalStatus = MedicalStatus.Fit;
        AdditionalInfo = string.IsNullOrWhiteSpace(additionalInfo) ? null : additionalInfo.Trim();
        SiteCode = NormaliseCode(siteCode);
    }

    public void DeclareNotFit(string? additionalInfo, string generatedCode)
    {
        if (Status is not SiteStatus.Pending)
            throw new InvalidOperationException($"Entry {Id} is in status {Status}; cannot declare not fit.");

        if (string.IsNullOrWhiteSpace(generatedCode))
            throw new ArgumentException("Generated site code is required.", nameof(generatedCode));

        Status = SiteStatus.Denied;
        MedicalStatus = MedicalStatus.NotFit;
        AdditionalInfo = string.IsNullOrWhiteSpace(additionalInfo) ? null : additionalInfo.Trim();
        SiteCodeGenerated = generatedCode.Trim().ToUpperInvariant();
    }

    public bool TrySubmitSiteCode(string submittedCode, string? additionalInfo)
    {
        if (Status is not SiteStatus.Denied)
            throw new InvalidOperationException($"Entry {Id} is in status {Status}; only denied entries can submit a code.");

        if (string.IsNullOrWhiteSpace(SiteCodeGenerated))
            throw new InvalidOperationException($"Entry {Id} has no generated site code on record.");

        var normalised = NormaliseCode(submittedCode);
        if (!string.Equals(normalised, SiteCodeGenerated, StringComparison.Ordinal))
        {
            SiteCode = normalised;
            return false;
        }

        Status = SiteStatus.OnSiteConditional;
        MedicalStatus = MedicalStatus.Conditional;
        SiteCode = normalised;

        if (!string.IsNullOrWhiteSpace(additionalInfo))
            AdditionalInfo = additionalInfo.Trim();

        return true;
    }

    private static string? NormaliseCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return code.Trim().ToUpperInvariant();
    }
}
