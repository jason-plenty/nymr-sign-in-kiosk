using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using NymrSignIn.Application.Register;

namespace NymrSignIn.Infrastructure.BlobStorage;

public sealed class SignatureStorageService : ISignatureStorage
{
    private readonly BlobContainerClient _containerClient;

    public SignatureStorageService(BlobServiceClient blobServiceClient, IOptions<BlobStorageOptions> options)
    {
        _containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
    }

    public async Task<string> UploadSignatureAsync(
        Guid entryId,
        DateOnly date,
        Stream imageStream,
        CancellationToken cancellationToken)
    {
        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blobName = $"{date:yyyy-MM-dd}/{entryId}.png";
        var blobClient = _containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(
            imageStream,
            new BlobHttpHeaders { ContentType = "image/png" },
            cancellationToken: cancellationToken);

        return blobClient.Uri.ToString();
    }
}
