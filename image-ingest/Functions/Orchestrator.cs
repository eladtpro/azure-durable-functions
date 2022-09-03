namespace ImageIngest.Functions;
public class Orchestrator
{
    public static long ZipBatchSizeMB { get; set; } =
        long.TryParse(System.Environment.GetEnvironmentVariable("ZipBatchSizeMB"), out long size) ? size : 10485760;

    [FunctionName(nameof(Orchestrator))]
    public static async Task Run(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        //1. Check for ready batch files    ++++++++++++++++++++++++++++++++++++++
        log.LogInformation($"C# OrchestrationTrigger trigger function Orchestrator called");
        ActivityAction activity = context.GetInput<ActivityAction>();
        activity.QueryStatus = BlobStatus.Pending;
        activity = await context.CallActivityAsync<ActivityAction>(nameof(Checker), activity);

        if (activity.Total.Bytes2Megabytes() < ZipBatchSizeMB) return;

        //2. Create batch id
        //TODO: use durable entity
        // EntityId entityId = new EntityId(nameof(DurableBatchCounter), activity.Namespace);
        // var batchCounter = await context.CallEntityAsync<IDurableBatchCounter>(entityId, nameof(IDurableBatchCounter.Enlist));
        // var batchCounter2 = await context.CallEntityAsync<DurableBatchCounter>(entityId, nameof(DurableBatchCounter.Enlist));
        activity.OverrideBatchId = $"{activity.Namespace}-{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}";

        //3. Zip Files
        activity = await context.CallActivityAsync<ActivityAction>(nameof(Zipper), activity);

        log.LogInformation($"Zipper: zip file stored successsfuly: {activity}");
    }
}
