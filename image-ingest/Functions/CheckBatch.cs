
namespace ImageIngest.Functions;
public static class CheckBatch
{

    [FunctionName(nameof(CheckBatch))]
    public static async Task<string> Run(
        [ActivityTrigger] ActivityAction activity,
        [DurableClient] IDurableEntityClient client,
        ILogger log)
    {
        EntityId entityId = new EntityId(nameof(IDurableStorage), activity.Namespace);
        EntityStateResponse<IDurableStorage> state = await client.ReadEntityStateAsync<IDurableStorage>(entityId);
        IList<ImageMetadata> batch = state.EntityState.Get().Values.Where(
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