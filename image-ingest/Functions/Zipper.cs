using System.Collections.Concurrent;
using System.IO.Packaging;

namespace ImageIngest.Functions;
public static class Zipper
{
    private static string AzureWebJobsFTPStorage =>
        System.Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");

    [FunctionName("Zipper")]
    public static async Task<ActivityAction> Run(
        [ActivityTrigger] ActivityAction activity,
        [Blob("zip/{activity.OverrideBatchId}.zip", FileAccess.Write, Connection = "AzureWebJobsZipStorage")] Stream blob,
        [Blob("images", Connection = "AzureWebJobsFTPStorage")] BlobContainerClient client,
        ILogger log)
    {
        log.LogInformation($"Zipper:ActivityTrigger trigger function Processed blob\n activity:{activity}");

        activity.OverrideStatus = BlobStatus.Batched;
        IDictionary<string, Tuple<BlobClient, BlobTags, Stream>> jobs = new ConcurrentDictionary<string, Tuple<BlobClient, BlobTags, Stream>>();

        await foreach (BlobTags tags in client.QueryAsync(activity.QueryStatusAndNamespace))
        {
            BlobClient blobClient = new BlobClient(AzureWebJobsFTPStorage, client.Name, tags.Name);
            jobs[tags.Name] = new Tuple<BlobClient, BlobTags, Stream>(blobClient, tags, null);
        }

        IList<Task> tasks = new List<Task>();
        foreach (var job in jobs)
        {
            var item = job;
            item.Value.Item2.Modified = DateTime.Now.ToFileTimeUtc();
            var task = item.Value.Item1.WriteTagsAsync(item.Value.Item2)
                .ContinueWith(r => item.Value.Item1.DownloadToAsync(item.Value.Item3));
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        using (Package zip = System.IO.Packaging.Package.Open(blob, FileMode.OpenOrCreate))
        {
            foreach (var item in jobs)
            {
                if (null == item.Value.Item3)
                {
                    log.LogError($"Cannot compress {item.Key}, Details: {item.Value.Item2}");
                    continue;
                }
                string destFilename = ".\\" + Path.GetFileName(item.Key);
                Uri uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
                if (zip.PartExists(uri)) zip.DeletePart(uri);

                PackagePart part = zip.CreatePart(uri, "", CompressionOption.NotCompressed);
                using (Stream dest = part.GetStream())
                    item.Value.Item3.CopyTo(dest);
            }
        }

        foreach (var job in jobs)
        {
            var item = job;
            item.Value.Item2.Modified = DateTime.Now.ToFileTimeUtc();
            item.Value.Item2.Status = BlobStatus.Zipped;
            var task = item.Value.Item1.WriteTagsAsync(item.Value.Item2);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        return activity;
    }
}
