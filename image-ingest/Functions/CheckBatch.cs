
namespace ImageIngest.Functions;
public static class CheckBatch
{

    [FunctionName("CheckBatch")]
    public static async Task<string> Run(
        [ActivityTrigger] string @namespace,
        [DurableClient] IDurableEntityClient client,
        ILogger log)
    {
        EntityId entityId = new EntityId(nameof(DurableStorage), @namespace);
        EntityStateResponse<DurableStorage> state = await client.ReadEntityStateAsync<DurableStorage>(entityId);
        IList<ImageMetadata> batch = state.EntityState.Images.Values.Where(
            v => v.Status == ImageStatus.Batched)
            .ToList();
        if (batch.Count < 1)
            return null;

        log.LogInformation($"found entities: {batch.Select(v => v.Name).ToArray()}");

        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssffff");
        await client.SignalEntityAsync<IDurableStorage>(entityId, proxy => proxy.UpdateAll(
            new ActivityAction
            {
                CurrentBatchId = null,
                OverrideBatchId = timestamp,
                CurrentStatus = ImageStatus.Batched,
                OverrideStatus = ImageStatus.Marked
            }));

        return timestamp;
    }
}