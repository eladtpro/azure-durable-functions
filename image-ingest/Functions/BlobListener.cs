namespace ImageIngest.Functions;
public class BlobListener
{
    [FunctionName(nameof(BlobListener))]
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
        EntityId entityId = new EntityId(nameof(IDurableStorage), metadata.Namespace);
        await client.SignalEntityAsync<IDurableStorage>(entityId, proxy => proxy.Upsert(metadata));
        log.LogInformation($"Upsert entity: {metadata}, calling Orchestrator");

        ActivityAction activity = new ActivityAction { Namespace = metadata.Namespace };
        await context.CallActivityAsync(nameof(Orchestrator), activity);
    }
}
