using Azure.Storage.Blobs;

namespace ImageIngest.Functions;
public class Cleaner
{
    [FunctionName("Cleaner")]
    public async Task<bool> Run(
        [BlobTrigger("zip/{name}", Connection = "AzureWebJobsZipStorage")] Stream blob, string name,
        [Blob("images", Connection = "AzureWebJobsStorage")] BlobContainerClient blobContainerClient,
        [DurableClient] IDurableEntityClient client,
        ILogger log)
    {
        log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blob.Length} Bytes");

        string[] parts = name.Split('.', 2);
        if (parts.Length < 2)
            throw new ArgumentException("name must include namespace and batch comma seperated", "name");

        string @namespace = parts[0];
        string batchId = parts[1];

        EntityId entityId = new EntityId(nameof(DurableStorage), @namespace);
        EntityStateResponse<DurableStorage> state = await client.ReadEntityStateAsync<DurableStorage>(entityId);
        IList<ImageMetadata> batch = state.EntityState.Images.Values.Where(
            v => v.Status == ImageStatus.Zipped && v.BatchId == batchId)
            .ToList();
        if (batch.Count < 1)
            return false;

        List<Task> tasks = new List<Task>();
        foreach (ImageMetadata item in batch)
        {
            Task<Azure.Response<bool>> task = blobContainerClient.DeleteBlobIfExistsAsync(item.Name);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        await client.SignalEntityAsync<IDurableStorage>(entityId, proxy => proxy.UpdateAll(
        new ActivityAction
        {
            CurrentBatchId = batchId,
            CurrentStatus = ImageStatus.Zipped,
            OverrideStatus = ImageStatus.Deleted
        }));

        return true;
    }
}
