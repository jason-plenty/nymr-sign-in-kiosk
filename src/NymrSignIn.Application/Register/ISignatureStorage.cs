namespace NymrSignIn.Application.Register;

public interface ISignatureStorage
{
    Task<string> UploadSignatureAsync(Guid entryId, DateOnly date, Stream imageStream, CancellationToken cancellationToken);
}
