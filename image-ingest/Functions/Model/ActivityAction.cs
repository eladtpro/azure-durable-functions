namespace ImageIngest.Functions.Model;

public class ActivityAction
{
    public string ZipName => $"{Namespace}.{CurrentBatchId}";
    public string Namespace { get; set; }
    public string CurrentBatchId { get; set; }
    public string OverrideBatchId { get; set; }
    public ImageStatus CurrentStatus { get; set; }
    public ImageStatus OverrideStatus { get; set; }
    public IDictionary<string, Stream> Images { get; set; }

    public override string ToString()
    {
        return $"CurrentBatchId: {CurrentBatchId}, OverrideBatchId: {OverrideBatchId}, CurrentStatus: {CurrentStatus}, OverrideStatus: {OverrideStatus}, Namespace: {Namespace}";
    }
}