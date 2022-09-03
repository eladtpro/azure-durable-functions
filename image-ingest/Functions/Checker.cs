
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
        {
            taggedBlobItem.Tags.TryGetValue(nameof(BlobTags.Length), out string length);
            activity.Total += long.TryParse(length, out long l) ? l : 0;
        }

        return activity;
    }
}