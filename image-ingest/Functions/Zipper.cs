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
        activity.QueryBatchId = activity.OverrideBatchId;
        log.LogInformation($"[Zipper] QueryAsync activity:{activity}");
        IDictionary<string, Tuple<BlobClient, BlobTags, Stream>> jobs = new ConcurrentDictionary<string, Tuple<BlobClient, BlobTags, Stream>>();
        await foreach (BlobTags tags in client.QueryAsync(t =>
            t.Status == activity.QueryStatus &&
            t.BatchId == activity.QueryBatchId &&
            t.Namespace == activity.Namespace))
        {
            BlobClient blobClient = new BlobClient(AzureWebJobsFTPStorage, client.Name, tags.Name);
            //var lease = blobClient.GetBlobLeaseClient();
            jobs[tags.Name] = new Tuple<BlobClient, BlobTags, Stream>(blobClient, tags, null);
        }

        //download file streams
        await Task.WhenAll(jobs.Select(item => item.Value.Item1.DownloadToAsync(item.Value.Item3)));
        log.LogInformation($"[Zipper] Downloaded {jobs.Count} blobs. Files: {string.Join(",", jobs.Select(t => $"{t.Key} ({t.Value.Item2.Length.Bytes2Megabytes()}MB)"))}");
        string currentKey = string.Empty;
        try
        {
            using (MemoryStream zipStream = new MemoryStream())
            {
                using (Package zip = System.IO.Packaging.Package.Open(zipStream, FileMode.OpenOrCreate))
                {
                    foreach (var item in jobs)
                    {
                        currentKey = item.Key;
                        if (null == item.Value.Item3)
                        {
                            log.LogError($"[Zipper] Cannot compress {item.Key}, Missing stream: {item.Value.Item2}");
                            item.Value.Item2.Status = BlobStatus.Error;
                            item.Value.Item2.Text = "Cannot compress failed downloading stream";
                            continue;
                        }
                        string destFilename = ".\\" + Path.GetFileName(item.Key);
                        Uri uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
                        if (zip.PartExists(uri)) zip.DeletePart(uri);

                        PackagePart part = zip.CreatePart(uri, "", CompressionOption.NotCompressed);
                        using (Stream dest = part.GetStream())
                            await item.Value.Item3.CopyToAsync(dest);
                    }
                    activity.OverrideStatus = BlobStatus.Zipped;
                }
                await zipStream.CopyToAsync(blob);
            }
        }
        catch (System.Exception ex)
        {
            log.LogError(ex, $"{ex.Message} Details: {activity}");
            if (jobs.TryGetValue(currentKey, out Tuple<BlobClient, BlobTags, Stream> tuple))
                tuple.Item2.Text = ex.Message;
            activity.OverrideStatus = BlobStatus.Error;
        }

        log.LogInformation($"[Zipper] Zip file completed, post creation marking blobs for deletion. Activity: {activity}");
        await Task.WhenAll(jobs.Select(job => job.Value.Item1.WriteTagsAsync(job.Value.Item2, t => t.Status = activity.OverrideStatus)));
        log.LogInformation($"[Zipper] Tags marked {jobs.Count} blobs. Status: {activity.OverrideStatus}, OverrideBatchId: {activity.OverrideBatchId}. Files: {string.Join(",", jobs.Select(t => $"{t.Key} ({t.Value.Item2.Length.Bytes2Megabytes()}MB)"))}");
        return activity;
    }
}
