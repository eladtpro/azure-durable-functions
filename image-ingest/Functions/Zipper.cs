using System.IO.Packaging;

namespace ImageIngest.Functions;
public static class Zipper
{
    private static string AzureWebJobsFTPStorage => Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");
    private static TimeSpan LeaseDuration => TimeSpan.Parse(Environment.GetEnvironmentVariable("LeaseDuration"));

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
        List<BatchJob> jobs = new List<BatchJob>();
        await foreach (BlobTags tags in client.QueryAsync(t =>
            t.Status == activity.QueryStatus &&
            t.BatchId == activity.QueryBatchId &&
            t.Namespace == activity.Namespace))
        {
            BatchJob job = new BatchJob(tags);
            jobs.Add(job);
        }

        if (jobs.Count < 1)
        {
            log.LogWarning($"[Zipper] No blobs found for activity:{activity}");
            return activity;
        }

        //download file streams
        await Task.WhenAll(jobs.Select(job => job.LeaseClient.AcquireAsync(LeaseDuration).ContinueWith(j => job.Lease = j.Result, TaskContinuationOptions.ExecuteSynchronously)));
        await Task.WhenAll(jobs.Select(job => job.BlobClient.DownloadToAsync(job.Stream, new BlobDownloadToOptions { Conditions = new BlobRequestConditions { LeaseId = job.Lease.LeaseId } })
            .ContinueWith(r => log.LogInformation($"[Zipper] Downloaded {job.BlobClient.Name}, length: {job.Stream.Length}, Success: {r.IsCompletedSuccessfully}, Exception: {r.Exception?.Message}"))
        ));

        log.LogInformation($"[Zipper] Downloaded {jobs.Count} blobs. Files: {string.Join(",", jobs.Select(j => $"{j.Name} ({j.Stream.Length})"))}");
        string currentJobName = string.Empty;
        try
        {
            using (MemoryStream zipStream = new MemoryStream())
            {
                using (Package zip = Package.Open(zipStream, FileMode.OpenOrCreate))
                {
                    foreach (var job in jobs)
                    {
                        currentJobName = job.Name;
                        if (null == job.Stream)
                        {
                            log.LogError($"[Zipper] Cannot compress part, no stream created: {job}");
                            job.Tags.Status = BlobStatus.Error;
                            job.Tags.Text = "Cannot compress failed downloading stream";
                            continue;
                        }
                        string destFilename = ".\\" + Path.GetFileName(job.Name);
                        Uri uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
                        if (zip.PartExists(uri)) zip.DeletePart(uri);

                        PackagePart part = zip.CreatePart(uri, "", CompressionOption.NotCompressed);
                        using (Stream dest = part.GetStream())
                            await job.Stream.CopyToAsync(dest);
                    }
                    activity.OverrideStatus = BlobStatus.Zipped;
                }
                await zipStream.CopyToAsync(blob);
            }
        }
        catch (System.Exception ex)
        {
            log.LogError(ex, $"{ex.Message} Details: {activity}");
            activity.OverrideStatus = BlobStatus.Error;
            var job = jobs.FirstOrDefault(j => j.Name == currentJobName);
            if (null != job)
            {
                job.Tags.Status = BlobStatus.Error;
                job.Tags.Text = ex.Message;
            }
        }

        log.LogInformation($"[Zipper] Zip file completed, post creation marking blobs for deletion. Activity: {activity}");
        await Task.WhenAll(jobs.Select(job => job.BlobClient
            .WriteTagsAsync(job.Tags, job.Lease.LeaseId, t => t.Status = activity.OverrideStatus)
            .ContinueWith(t => job.LeaseClient.ReleaseAsync())));
        log.LogInformation($"[Zipper] Tags marked {jobs.Count} blobs. Status: {activity.OverrideStatus}, OverrideBatchId: {activity.OverrideBatchId}. Files: {string.Join(",", jobs.Select(t => $"{t.Name} ({t.Tags.Length.Bytes2Megabytes()}MB)"))}");
        return activity;
    }
}
