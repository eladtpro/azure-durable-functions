namespace ImageIngest.Functions;
public class Orchestrator
{
    [FunctionName(nameof(Orchestrator))]
    public async Task Run([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
    {
        //1. Check for ready batch files    ++++++++++++++++++++++++++++++++++++++
        log.LogInformation($"C# OrchestrationTrigger trigger function Orchestrator called");
        ActivityAction activity = context.GetInput<ActivityAction>();
        string batchId = await context.CallActivityAsync<string>(nameof(CheckBatch), activity);
        if (string.IsNullOrWhiteSpace(batchId))
            return;

        //2. Download files                 ++++++++++++++++++++++++++++++++++++++
        EntityId entityId = new EntityId(nameof(DurableStorage), activity.Namespace);
        IDictionary<string, ImageMetadata> images = await context.CallEntityAsync<IDictionary<string, ImageMetadata>>(entityId, "Get");
        IList<ImageMetadata> batch = images.Values.Where(v => (v.Status == ImageStatus.Marked && v.BatchId == batchId)).ToList();
        if (batch.Count < 1) return;

        Dictionary<string, Task<Stream>> files = new Dictionary<string, Task<Stream>>();

        //List<Tuple<ImageMetadata, Task<Stream>>> items = new List<Tuple<ImageMetadata, Task<Stream>>>();
        foreach (var item in batch)
        {
            Task<Stream> task = context.CallActivityAsync<Stream>(nameof(Downloader), item.Name);
            files.Add(item.Name, task);
        }
        await Task.WhenAll(files.Values);

        activity.Images = files.ToDictionary(entry => entry.Key, entry => entry.Value.IsCompletedSuccessfully ? null : entry.Value.Result);
        activity.CurrentBatchId = batchId;

        //3. Zip Files
        string zipFile = await context.CallActivityAsync<string>(nameof(Zipper), activity);
        log.LogInformation($"zip file stored successsfuly: {zipFile}");

    }
}
