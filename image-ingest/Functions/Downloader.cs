namespace ImageIngest.Functions;
public static class Downloader
{

    [FunctionName(nameof(Downloader))]
    public static Stream Run(
        [ActivityTrigger] string name,
        [Blob("images/{name}", FileAccess.Write, Connection = "AzureWebJobsStorage")] Stream blob,
        //IBinder binder,
        ILogger log)
    {
        // await blobClient.DownloadStreamingAsync() .UploadAsync() .UploadFromStreamAsync(fileStream);
        // var blobs = await binder.BindAsync<IEnumerable<CloudBlockBlob>>(new BlobAttribute(blobPath: $"activities/migration")
        // {
        //     Connection = "AzureWebJobsStorage"
        // });
        return blob;
    }
}

