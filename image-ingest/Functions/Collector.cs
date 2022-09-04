
namespace ImageIngest.Functions;
public class Collector
{
    public static string AzureWebJobsFTPStorage { get; set; } = System.Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");
    public static long ZipBatchSizeMB { get; set; } = long.TryParse(System.Environment.GetEnvironmentVariable("ZipBatchSizeMB"), out long size) ? size : 10485760;


    [FunctionName(nameof(Collector))]
    public static async Task<ActivityAction> Run(
        [ActivityTrigger] ActivityAction activity,
        [Blob("images", Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        ILogger log)
    {
        log.LogInformation($"C# Blob trigger function Processed blob\n activity:{activity}");
        log.LogInformation(activity.QueryStatusAndNamespace);

        // await foreach (var tag in blobContainerClient.QueryAsync(activity.QueryStatusAndNamespace))
        List<BlobTags> tags = new List<BlobTags>();
        await foreach (BlobTags tag in blobContainerClient.QueryAsync(t => t.Status == activity.QueryStatus && t.Namespace == activity.Namespace))
        {
            activity.Total += tag.Length;
            tags.Add(tag);
        }

        if (activity.Total.Bytes2Megabytes() < ZipBatchSizeMB) return activity;
        //Create batch id
        //TODO: use durable entity
        // EntityId entityId = new EntityId(nameof(DurableBatchCounter), activity.Namespace);
        // var batchCounter = await context.CallEntityAsync<IDurableBatchCounter>(entityId, nameof(IDurableBatchCounter.Enlist));
        // var batchCounter2 = await context.CallEntityAsync<DurableBatchCounter>(entityId, nameof(DurableBatchCounter.Enlist));
        activity.OverrideBatchId = $"{activity.Namespace}-{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}";
        activity.OverrideStatus = BlobStatus.Batched;
        await Task.WhenAll(tags.Select(tag =>
            new BlobClient(AzureWebJobsFTPStorage, tag.Container, tag.Name).WriteTagsAsync(tag, t =>
            {
                t.Status = activity.OverrideStatus;
                t.BatchId = activity.OverrideBatchId;
            })
        ));

        return activity;
    }
}