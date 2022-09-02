
namespace ImageIngest.Functions;
public static class Checker
{
    [FunctionName(nameof(Checker))]
    public static async Task<ActivityAction> Run(
        [ActivityTrigger] ActivityAction activity,
        [Blob("images", Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        ILogger log)
    {
        log.LogInformation($"C# Blob trigger function Processed blob\n activity:{activity}");
        log.LogInformation(activity.QueryStatusAndNamespace);
        await foreach (TaggedBlobItem taggedBlobItem in blobContainerClient.FindBlobsByTagsAsync(activity.QueryStatusAndNamespace))
            activity.Total += long.TryParse(taggedBlobItem.Tags[nameof(BlobTags.Length)], out long length) ? length : 0;

        return activity;
    }
}