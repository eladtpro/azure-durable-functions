
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

        await foreach (var tag in blobContainerClient.QueryAsync(activity.QueryStatusAndNamespace))
            activity.Total += tag.Length; ;
        return activity;
    }
}