namespace ImageIngest.Functions
{
    public class Scavenger
    {
        public int ScavengerPageSize { get; set; } =
            int.TryParse(System.Environment.GetEnvironmentVariable("ScavengerPageSize"), out int size) ? size : 10485760;

        public TimeSpan ScavengerOutdatedThreshold { get; set; } =
            TimeSpan.TryParse(System.Environment.GetEnvironmentVariable("ScavengerOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

        [FunctionName("Scavenger")]
        public async Task Run(
            [TimerTrigger("0 * * * * *")] TimerInfo myTimer,
            [Blob("images", Connection = "AzureWebJobsFTPStorage")] BlobContainerClient blobContainerClient,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            ActivityAction activity = new ActivityAction() { QueryStatus = BlobStatus.Zipped };

            await foreach (BlobTags tags in blobContainerClient.QueryAsync(t => 
                t.Status == BlobStatus.Zipped && 
                t.Modified < DateTime.UtcNow.Add(ScavengerOutdatedThreshold).ToFileTimeUtc()))
                await blobContainerClient.DeleteBlobIfExistsAsync(tags.Name);
            log.LogInformation($"Deleted Zipped files");

            await foreach (BlobTags tags in blobContainerClient.QueryAsync(t => 
                t.Status ==  BlobStatus.Pending && 
                t.Modified < DateTime.UtcNow.Add(ScavengerOutdatedThreshold).ToFileTimeUtc()))
                log.LogWarning($"Found pending file {tags.Name}, tags: {tags.Tags}");
        }
    }
}

