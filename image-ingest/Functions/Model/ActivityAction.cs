namespace ImageIngest.Functions.Model;

public class ActivityAction
{
    public static TimeSpan Threshold =
        TimeSpan.TryParse(System.Environment.GetEnvironmentVariable("ScavengerOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

    public ActivityAction() { }

    public ActivityAction(BlobTags tags)
    {
        CurrentStatus = tags.Status;
        Namespace = tags.Namespace;
    }

    public long Total { get; set; }
    public string Namespace { get; set; } = "default";
    public string CurrentBatchId { get; set; }
    public string OverrideBatchId { get; set; }
    public BlobStatus CurrentStatus { get; set; }
    public BlobStatus OverrideStatus { get; set; }

    public string QueryStatusAndNamespace =>
        $@"""Status""='{CurrentStatus.ToString()}' AND ""Namespace""= '{Namespace}'";

    public string QueryStatusAndThreshold =>
        $@"""Status""='{CurrentStatus.ToString()}' AND ""Modified"" < '{DateTime.UtcNow.Add(Threshold).ToFileTimeUtc()}'";

    public override string ToString()
    {
        return $"{Total}, CurrentBatchId: {CurrentBatchId}, OverrideBatchId: {OverrideBatchId}, CurrentStatus: {CurrentStatus}, OverrideStatus: {OverrideStatus}, Namespace: {Namespace}";
    }
}
