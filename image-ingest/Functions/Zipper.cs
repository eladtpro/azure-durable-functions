
using System.IO.Packaging;

namespace ImageIngest.Functions;
public static class Zipper
{

    [FunctionName("Zipper")]
    public static async Task<string> Run(
        [ActivityTrigger] ActivityAction activity,
        [Blob("zip/{activity.ZipName}.zip", FileAccess.Write, Connection = "AzureWebJobsZipStorage")] Stream blob, string name,
        [DurableClient] IDurableEntityClient client,
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        EntityId entityId = new EntityId(nameof(DurableStorage), activity.Namespace);
        EntityStateResponse<DurableStorage> state = await client.ReadEntityStateAsync<DurableStorage>(entityId);
        IList<ImageMetadata> batch = state.EntityState.Images.Values.Where(
            v => (v.Status == ImageStatus.Marked && v.BatchId == activity.OverrideBatchId)
            )
            .ToList();
        if (batch.Count < 1)
            return null;

        List<Tuple<ImageMetadata, Task<Stream>>> items = new List<Tuple<ImageMetadata, Task<Stream>>>();

        foreach (var item in batch)
        {
            string batchId = await context.CallActivityAsync<string>("CheckBatch", item.Name);

            Task<Stream> task = context.CallActivityAsync<Stream>("Downloader", item.Name);
            items.Add(new Tuple<ImageMetadata, Task<Stream>>(item, task));
        }

        await Task.WhenAll(items.Select(t => t.Item2));

        using (Package zip = System.IO.Packaging.Package.Open(blob, FileMode.OpenOrCreate))
        {
            foreach (var item in items)
            {
                if(item.Item2.Status != TaskStatus.RanToCompletion){
                    log.LogError($"Cannot compress {item.Item1.Name}");
                    continue;
                }

                string destFilename = ".\\" + Path.GetFileName(item.Item1.Name);
                Uri uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
                if (zip.PartExists(uri)) zip.DeletePart(uri);

                PackagePart part = zip.CreatePart(uri, "", CompressionOption.NotCompressed);
                using (Stream dest = part.GetStream())
                    item.Item2.Result.CopyTo(dest);
            }
        }

        await client.SignalEntityAsync<DurableStorage>(entityId, proxy => proxy.UpdateAll(
            new ActivityAction
            {
                CurrentBatchId = activity.CurrentBatchId,
                CurrentStatus = ImageStatus.Marked,
                OverrideStatus = ImageStatus.Zipped
            }));

        return $"{name}.zip";
    }
}

