namespace ImageIngest.Functions.Model;

public class ActivityAction
{
    public static TimeSpan Threshold =
        TimeSpan.TryParse(System.Environment.GetEnvironmentVariable("ScavengerOutdatedThreshold"), out TimeSpan span) ? span : TimeSpan.FromMinutes(5);

    public ActivityAction() { }

    public ActivityAction(BlobTags tags)
    {
        QueryStatus = tags.Status;
        Namespace = tags.Namespace;
    }

    public long Total { get; set; }
    public string Namespace { get; set; } = "default";
    public string QueryBatchId { get; set; }
    public string OverrideBatchId { get; set; }
    public BlobStatus QueryStatus { get; set; }
    public BlobStatus OverrideStatus { get; set; }

    public string QueryStatusAndNamespace =>
        $@"""Status""='{QueryStatus.ToString()}' AND ""Namespace""= '{Namespace}'";

    public string QueryStatusAndThreshold =>
        $@"""Status""='{QueryStatus.ToString()}' AND ""Modified"" < '{DateTime.UtcNow.Add(Threshold).ToFileTimeUtc()}'";

    public override string ToString()
    {
        return $"{Total}, CurrentBatchId: {QueryBatchId}, OverrideBatchId: {OverrideBatchId}, CurrentStatus: {QueryStatus}, OverrideStatus: {OverrideStatus}, Namespace: {Namespace}";
    }
}
