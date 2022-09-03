namespace ImageIngest.Functions.Model;
public class BlobTags
{
    public BlobTags() { }

    public BlobTags(BlobProperties props)
    {
        Status = BlobStatus.Pending;
        Length = props.ContentLength;
    }

    public BlobTags(TaggedBlobItem item)
    {
        BatchId = item.Tags[nameof(BatchId)];
        Namespace = item.Tags[nameof(Namespace)];
        Status = Enum.TryParse<BlobStatus>(item.Tags[nameof(Status)], true, out BlobStatus status) ? status : BlobStatus.Pending;
        Length = long.TryParse(item.Tags[nameof(Length)], out long length) ? length : 0;
        Created = long.TryParse(item.Tags[nameof(Created)], out long created) ? created : DateTime.Now.ToFileTimeUtc();
    }

    public long Created { get; set; } = DateTime.Now.ToFileTimeUtc();
    public long Modified { get; set; } = DateTime.Now.ToFileTimeUtc();
    public BlobStatus Status { get; set; } = BlobStatus.New;
    public string Container { get; set; }
    public string Namespace { get; set; } = "default";
    public string BatchId { get; set; } = string.Empty;
    public long Length { get; set; }

    public IDictionary<string, string> Tags => new Dictionary<string, string>
        {
            { nameof(BatchId), BatchId },
            { nameof(Namespace), Namespace},
            { nameof(Status), Status.ToString() },
            { nameof(Length), Length.ToString() },
            { nameof(Created), Created.ToString() },
            { nameof(Modified), Modified.ToString() },
        };

    public override string ToString() =>
        $"Namespace: {Namespace}, BatchId: {BatchId}, Length: {Length}, Created: {Created}, Modified: {Modified}";
}