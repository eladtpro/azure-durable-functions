namespace ImageIngest.Functions;
public class ZipListener
{
    [FunctionName(nameof(ZipListener))]
    public async Task Run(
        [BlobTrigger("zip/{name}.zip", Connection = "AzureWebJobsZipStorage")] BlobClient blobClient,
        [Blob("images", Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        ILogger log)
    {
        log.LogInformation($"[ZipListener] Function triggered on blob {blobClient.Name}");
        ActivityAction activity = ActivityAction.ExtractBatchIdAndNamespace(blobClient.Name);
        activity.QueryStatus = BlobStatus.Zipped;

        log.LogInformation($"[ZipListener] Delete by query '{activity.QueryStatusAndNamespace}'. Details {activity}");
        await blobContainerClient.DeleteByQueryAsync(activity.QueryStatusAndNamespace);
    }
}
