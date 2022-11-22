namespace ImageIngest.Functions
{
    public class Scavenger
    {
        public int ScavengerPageSize { get; set; } =
            int.TryParse(System.Environment.GetEnvironmentVariable("ScavengerPageSize"), out int size) ? size : 10485760;

        public TimeSpan ScavengerOutdatedThreshold { get; set; } =
            TimeSpan.TryParse(System.Environment.GetEnvironmentVariable("ScavengerOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

        [FunctionName(nameof(Scavenger))]
        public async Task Run(
            [TimerTrigger("0 * * * * *")] TimerInfo myTimer,
            [Blob(ActivityAction.ContainerName, Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
            ILogger log)
        {
            log.LogInformation($"[Scavenger] Timer trigger function executed at: {DateTime.Now}");
            ActivityAction activity = new ActivityAction() { QueryStatus = BlobStatus.Zipped };

            List<BlobTags> zippedTags = new List<BlobTags>();
            await foreach (BlobTags tags in blobContainerClient.QueryAsync(t =>
                t.Status == BlobStatus.Zipped &&
                t.Modified < DateTime.UtcNow.Add(ScavengerOutdatedThreshold).ToFileTimeUtc()))
            {
                zippedTags.Add(tags);
                await blobContainerClient.DeleteBlobIfExistsAsync(tags.Name);
            }
            log.LogInformation($"[Scavenger] deleted {zippedTags.Count} zipped image blobs.\n {string.Join(",", zippedTags.Select(t => $"{t.Name} ({t.Length.Bytes2Megabytes()}MB)"))}");

            await foreach (BlobTags tags in blobContainerClient.QueryAsync(t =>
                t.Status == BlobStatus.Pending &&
                t.Modified < DateTime.UtcNow.Add(ScavengerOutdatedThreshold).ToFileTimeUtc()))
                log.LogWarning($"[Scavenger] Found pending old file {tags.Name}, tags: {tags.Tags}");
        }
    }
}

