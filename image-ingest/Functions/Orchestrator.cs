namespace ImageIngest.Functions;
public class Orchestrator
{

    [FunctionName(nameof(Orchestrator))]
    public static async Task Run(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        //1. Check for ready batch files    ++++++++++++++++++++++++++++++++++++++
        log.LogInformation($"C# OrchestrationTrigger trigger function Orchestrator called");
        ActivityAction activity = context.GetInput<ActivityAction>();
        activity.QueryStatus = BlobStatus.Pending;
        activity = await context.CallActivityAsync<ActivityAction>(nameof(Collector), activity);

        //Check if batch created
        if (!string.IsNullOrWhiteSpace(activity.OverrideBatchId))
            return;

        //3. Zip Files
        activity = await context.CallActivityAsync<ActivityAction>(nameof(Zipper), activity);
        log.LogInformation($"Zipper: zip file stored successsfuly: {activity}");
    }
}
