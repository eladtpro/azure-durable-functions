using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace ImageIngest.Functions;

[JsonObject(MemberSerialization.OptIn)]
public class DurableStorage : IDurableStorage
{
    public IDictionary<string, ImageMetadata> Images { get; set; } =
        new ConcurrentDictionary<string, ImageMetadata>();

    public long BatchSize { get; set; } =
        int.TryParse(System.Environment.GetEnvironmentVariable("BatchSize"), out int size) ? size : 10485760;

    public void Upsert(ImageMetadata metadata)
    {
        this.Images[metadata.Name.Sanitize()] = metadata;
        long total = this.Images.Values.Sum(v =>
            v.Status == ImageStatus.Pending ? v.Length : 0);
        if (total > BatchSize)
            this.UpdateAll(new ActivityAction
            {
                CurrentBatchId = metadata.BatchId,
                CurrentStatus = ImageStatus.Pending,
                OverrideStatus = ImageStatus.Batched
            });
        return;
    }

    public void UpdateAll(ActivityAction update)
    {
        this.Images.UpdateAll(
                (k, v) => v.Status == update.CurrentStatus &&
                (string.IsNullOrWhiteSpace(update.CurrentBatchId) ? true : v.BatchId == update.CurrentBatchId),
                v =>
                {
                    v.Status = update.OverrideStatus;
                    v.Modified = DateTime.Now;
                    if (!string.IsNullOrWhiteSpace(update.OverrideBatchId))
                        v.BatchId = update.OverrideBatchId;
                });
    }


    public void CLear() =>
        this.Images.Clear();

    public IDictionary<string, ImageMetadata> Get() =>
        this.Images;

    [FunctionName(nameof(DurableStorage))]
    public static Task Run([EntityTrigger] IDurableEntityContext ctx)
        => ctx.DispatchAsync<DurableStorage>();
}