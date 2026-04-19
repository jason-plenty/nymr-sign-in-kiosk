using NymrSignIn.Client.Register.Models;

namespace NymrSignIn.Client.Register.Services;

public sealed class SignInFlowState
{
    public SignInResponseModel? CurrentPerson { get; private set; }
    public DeclareNotFitResponseModel? NotFitContext { get; private set; }
    public ConfirmFitResponseModel? Confirmation { get; private set; }
    public string? AdditionalInfo { get; set; }
    public string? SiteCodeInput { get; set; }

    public void StartFlow(SignInResponseModel person)
    {
        CurrentPerson = person;
        NotFitContext = null;
        Confirmation = null;
        AdditionalInfo = null;
        SiteCodeInput = null;
    }

    public void SetNotFitContext(DeclareNotFitResponseModel context)
    {
        NotFitContext = context;
    }

    public void SetConfirmation(ConfirmFitResponseModel confirmation)
    {
        Confirmation = confirmation;
    }

    public void Reset()
    {
        CurrentPerson = null;
        NotFitContext = null;
        Confirmation = null;
        AdditionalInfo = null;
        SiteCodeInput = null;
    }
}
