namespace ImageIngest.Functions;
public class ZipListener
{
    [FunctionName(nameof(ZipListener))]
    public async Task Run(
        [BlobTrigger("zip/{name}.zip", Connection = "AzureWebJobsZipStorage")] BlobClient blobClient,
        [Blob("images", Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        ILogger log)
    {
        log.LogInformation($"C# Blob trigger function Processed blob\n Name:{blobClient.Name}");

        string batchId = Path.GetFileNameWithoutExtension(blobClient.Name);
        ActivityAction activity = new ActivityAction() { CurrentStatus = BlobStatus.Zipped, CurrentBatchId = batchId };
        await foreach (TaggedBlobItem taggedBlobItem in blobContainerClient.FindBlobsByTagsAsync(activity.QueryStatusAndNamespace))
            await blobContainerClient.DeleteBlobIfExistsAsync(taggedBlobItem.BlobName);
    }
}
