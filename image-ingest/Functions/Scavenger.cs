﻿namespace ImageIngest.Functions
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
            ActivityAction activity = new ActivityAction() { CurrentStatus = BlobStatus.Zipped };

            await foreach (BlobTags taggedBlobItem in blobContainerClient.QueryAsync(activity.QueryStatusAndThreshold))
                await blobContainerClient.DeleteBlobIfExistsAsync(taggedBlobItem.Name);
            log.LogInformation($"Deleted Zipped files");

            activity.CurrentStatus = BlobStatus.Pending;
            await foreach (BlobTags taggedBlobItem in blobContainerClient.QueryAsync(activity.QueryStatusAndThreshold))
                log.LogInformation($"Found pending file {taggedBlobItem.Name}, tags: {taggedBlobItem.Tags}");
     
            await foreach (BlobItem item in blobContainerClient.GetBlobsAsync(BlobTraits.Tags))
                log.LogInformation($"Found blob {item.Name}");
        }
    }
}

