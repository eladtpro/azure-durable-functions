using System.Collections.Concurrent;
using System.IO.Packaging;

namespace ImageIngest.Functions;
public static class Zipper
{
    private static string AzureWebJobsFTPStorage =>
        System.Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");

    [FunctionName(nameof(Zipper))]
    public static async Task<ActivityAction> Run(
        [ActivityTrigger] ActivityAction activity,
        [Blob("zip/{activity.OverrideBatchId}.zip", FileAccess.Write, Connection = "AzureWebJobsZipStorage")] Stream blob,
        [Blob("images", Connection = "AzureWebJobsFTPStorage")] BlobContainerClient client,
        ILogger log)
    {
        log.LogInformation($"[Zipper] ActivityTrigger trigger function Processed blob\n activity:{activity}");
        IDictionary<string, Tuple<BlobClient, BlobTags, Stream>> jobs = new ConcurrentDictionary<string, Tuple<BlobClient, BlobTags, Stream>>();
        await foreach (BlobTags tags in client.QueryAsync(t => t.Status == activity.QueryStatus && t.Namespace == activity.Namespace))
        {
            BlobClient blobClient = new BlobClient(AzureWebJobsFTPStorage, client.Name, tags.Name);
            //var lease = blobClient.GetBlobLeaseClient();
            jobs[tags.Name] = new Tuple<BlobClient, BlobTags, Stream>(blobClient, tags, null);
        }

        //download file streams
        await Task.WhenAll(jobs.Select(item => item.Value.Item1.DownloadToAsync(item.Value.Item3)));
        log.LogInformation($"[Zipper] Downloaded {jobs.Count} blobs. Files: {string.Join(",", jobs.Select(t => $"{t.Key} ({t.Value.Item2.Length.Bytes2Megabytes()}MB)"))}");

        using (Package zip = System.IO.Packaging.Package.Open(blob, FileMode.OpenOrCreate))
        {
            foreach (var item in jobs)
            {
                if (null == item.Value.Item3)
                {
                    log.LogError($"[Zipper] Cannot compress {item.Key}, Details: {item.Value.Item2}");
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
        
        activity.OverrideStatus = BlobStatus.Zipped;
        log.LogInformation($"[Zipper] Zip file completed, post creation marking blobs for deletion. Activity: {activity}");
        await Task.WhenAll(jobs.Select(job => job.Value.Item1.WriteTagsAsync(job.Value.Item2, t => t.Status = activity.OverrideStatus)));
        log.LogInformation($"[Zipper] Tags marked {jobs.Count} blobs. Status: {activity.OverrideStatus}, OverrideBatchId: {activity.OverrideBatchId}. Files: {string.Join(",", jobs.Select(t => $"{t.Key} ({t.Value.Item2.Length.Bytes2Megabytes()}MB)"))}");
        return activity;
    }
}
