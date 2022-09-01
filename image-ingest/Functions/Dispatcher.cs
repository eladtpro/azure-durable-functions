namespace ImageIngest.Functions;
public class Dispatcher
{
    [FunctionName("Dispatcher")]
    public async Task Run(
        [BlobTrigger("images/{name}", Source = BlobTriggerSource.EventGrid, Connection = "AzureWebJobsStorage")]
            Stream blob, string name,
        [DurableClient] IDurableEntityClient client,
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blob.Length} Bytes");

        ImageMetadata metadata = new ImageMetadata
        {
            Key = name.Sanitize(),
            //Namespace = bucket1, backet2 etc., 
            Status = ImageStatus.Pending,
            Length = blob.Length,
            Path = name,
            Name = name
        };

        log.LogInformation($"Original blob name: {name}  details: {metadata}");
        EntityId entityId = new EntityId(nameof(DurableStorage), metadata.Namespace);
        await client.SignalEntityAsync<DurableStorage>(entityId, proxy => proxy.Upsert(metadata));
        log.LogInformation($"Upsert entity: {metadata}");

        try
        {
            string batchId = await context.CallActivityAsync<string>(nameof(CheckBatch), metadata.Namespace);
            if (string.IsNullOrWhiteSpace(batchId))
                return;

            string zipFile = await context.CallActivityAsync<string>(nameof(Zipper), new ActivityAction
            {
                Namespace = metadata.Namespace,
                CurrentBatchId = batchId
            });
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Something went wrong");
        }
    }
}
