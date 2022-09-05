namespace ImageIngest.Functions;
public class Orchestrator
{

    [FunctionName(nameof(Orchestrator))]
    public static async Task Run(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        //1. Check for ready batch files    ++++++++++++++++++++++++++++++++++++++
        log.LogInformation($"[Orchestrator] OrchestrationTrigger triggered Function from [BlobListener] for InstanceId {context.InstanceId}");
        ActivityAction activity = context.GetInput<ActivityAction>();
        activity.QueryStatus = BlobStatus.Pending;
        log.LogInformation($"[Orchestrator] ActivityAction {activity}");
        activity = await context.CallActivityAsync<ActivityAction>(nameof(Collector), activity);

        //Check if batch created
        if (string.IsNullOrWhiteSpace(activity.OverrideBatchId))
        {
            log.LogInformation($"[Orchestrator] No batch created. ActivityAction {activity}");
            return;
        }

        //3. Zip Files
        activity.QueryStatus = BlobStatus.Batched;
        log.LogInformation($"[Orchestrator] Zipping files. ActivityAction {activity}");
        activity = await context.CallActivityAsync<ActivityAction>(nameof(Zipper), activity);
        log.LogInformation($"[Orchestrator] zip file stored successsfuly {activity}");
    }
}
