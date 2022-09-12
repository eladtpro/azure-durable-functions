using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace ImageIngest.Functions;

public class BlobListener
{
    [FunctionName(nameof(BlobListener))]
    public async Task Run(
        // [BlobTrigger("images/{name}", Source = BlobTriggerSource.EventGrid, Connection = "AzureWebJobsFTPStorage")] BlobClient blobClient,
        [EventGridTrigger] EventGridEvent blobEvent, 
        [Blob("images", Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        string blobName = blobEvent.Subject.Replace("/blobServices/default/containers/", string.Empty, StringComparison.InvariantCultureIgnoreCase);
        BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);
        log.LogInformation($"[BlobListener] Function triggered on blob {blobName}");
        BlobProperties props = await blobClient.GetPropertiesAsync();
        BlobTags tags = new BlobTags(props, blobClient);
        ActivityAction activity = new ActivityAction(tags);
        Response response = await blobClient.WriteTagsAsync(tags);
        if(response.IsError)
            log.LogError(new EventId(1001), response.ToString());
        log.LogInformation($"[BlobListener] BlobTags saved for blob {blobName}, Tags: {tags}");
        await starter.StartNewAsync<ActivityAction>(nameof(Orchestrator), activity);
    }
}
