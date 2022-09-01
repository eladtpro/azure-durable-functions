
using System.IO.Packaging;

namespace ImageIngest.Functions;
public static class Zipper
{
    [FunctionName("Zipper")]
    public static async Task<string> Run(
        [ActivityTrigger] ActivityAction activity,
        [Blob("zip/{activity.ZipName}.zip", FileAccess.Write, Connection = "AzureWebJobsZipStorage")] Stream blob, string name,
        [DurableClient] IDurableEntityClient client,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        EntityId entityId = new EntityId(nameof(DurableStorage), activity.Namespace);
        using (Package zip = System.IO.Packaging.Package.Open(blob, FileMode.OpenOrCreate))
        {
            foreach (var item in activity.Images)
            {
                if(null != item.Value){
                    log.LogError($"Cannot compress {item.Key}");
                    continue;
                }

                string destFilename = ".\\" + Path.GetFileName(item.Key);
                Uri uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
                if (zip.PartExists(uri)) zip.DeletePart(uri);

                PackagePart part = zip.CreatePart(uri, "", CompressionOption.NotCompressed);
                using (Stream dest = part.GetStream())
                    item.Value.CopyTo(dest);
            }
        }

        await client.SignalEntityAsync<IDurableStorage>(entityId, proxy => proxy.UpdateAll(
            new ActivityAction
            {
                CurrentBatchId = activity.CurrentBatchId,
                CurrentStatus = ImageStatus.Marked,
                OverrideStatus = ImageStatus.Zipped
            }));

        return $"{name}.zip";
    }
}

