namespace ImageIngest.Functions;
public class BlobListener
{
    [FunctionName(nameof(BlobListener))]
    public async Task Run(
        [BlobTrigger("images/{name}", /*Source = BlobTriggerSource.EventGrid, */Connection = "AzureWebJobsFTPStorage")]
            BlobClient blobClient,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        log.LogInformation($"C# Blob trigger function Processed blob\n Name:{blobClient.Name}");

        BlobProperties props = await blobClient.GetPropertiesAsync();
        BlobTags tags = new BlobTags(props);
        ActivityAction activity = new ActivityAction(tags);
        Response response = await blobClient.WriteTagsAsync(tags);
        if(response.IsError)
            log.LogError(new EventId(1001), response.ToString());
        await starter.StartNewAsync<ActivityAction>(nameof(Orchestrator), activity);
    }
}
