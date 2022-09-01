namespace ImageIngest.Functions;
public class Orchestrator
{
    [FunctionName(nameof(Orchestrator))]
    public async Task Run(
        [ActivityTrigger] ActivityAction activity,
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        log.LogInformation($"C# Blob trigger function Processed blob\n activity:{activity}");
        string batchId = await context.CallActivityAsync<string>(nameof(CheckBatch), activity.Namespace);
        if (string.IsNullOrWhiteSpace(batchId))
            return;

        string zipFile = await context.CallActivityAsync<string>(nameof(Zipper), new ActivityAction
        {
            Namespace = activity.Namespace,
            CurrentBatchId = batchId
        });
    }
}
