namespace ImageIngest.Functions.Model;
public class BlobTags
{
    private IDictionary<string, string> tags = new Dictionary<string, string>();
    public IDictionary<string, string> Tags => tags;

    public string BlobName { get; set; }
    public string BlobContainerName { get; set; }

    public long Created
    {
        get => tags.GetValue<long>(nameof(Created));
        set => tags[nameof(Created)] = value.ToString();
    }

    public long Modified
    {
        get => tags.GetValue<long>(nameof(Modified));
        set => tags[nameof(Modified)] = value.ToString();
    }

    public BlobStatus Status
    {
        get => (Enum.TryParse<BlobStatus>(tags[nameof(Status)], true, out BlobStatus status) ? status : BlobStatus.New);
        set => tags[nameof(Length)] = value.ToString();
    }

    public string Container
    {
        get => tags.GetValue<string>(nameof(Container));
        set => tags[nameof(Container)] = value;
    }

    public string Namespace
    {
        get => tags.GetValue<string>(nameof(Namespace));
        set => tags[nameof(Namespace)] = value;
    }

    public string BatchId
    {
        get => tags.TryGetValue(nameof(BatchId), out string batchId) ? batchId : string.Empty;
        set => tags[nameof(BatchId)] = value;
    }
    public long Length
    {
        get => tags.GetValue<long>(nameof(Length));
        set => tags[nameof(Length)] = value.ToString();
    }

    public void Initialize()
    {
        tags[nameof(Container)] = string.Empty;
        tags[nameof(Status)] = BlobStatus.Pending.ToString();
        tags[nameof(BatchId)] = string.Empty;
        tags[nameof(Namespace)] = "default"; ;
        tags[nameof(Length)] = "0";
        tags[nameof(Created)] = DateTime.Now.ToFileTimeUtc().ToString();
        tags[nameof(Modified)] = DateTime.Now.ToFileTimeUtc().ToString();
    }

    public BlobTags() { }
    public BlobTags(IDictionary<string, string> origin)
    {
        Initialize();
        origin.ToList().ForEach(x => tags[x.Key] = x.Value);
    }

    public BlobTags(BlobItem blobItem)
    {
        Initialize();
        blobItem.Tags.ToList().ForEach(x => tags[x.Key] = x.Value);
    }

    public BlobTags(BlobProperties props)
    {
        Initialize();
        tags[nameof(Status)] = BlobStatus.Pending.ToString();
        tags[nameof(Length)] = props.ContentLength.ToString();
    }

    public BlobTags(TaggedBlobItem item)
    {
        Initialize();
        item.Tags.ToList().ForEach(x => tags[x.Key] = x.Value);
        BlobName = item.BlobName;
        BlobContainerName = item.BlobContainerName;
    }

    public override string ToString() =>
        $"Namespace: {Namespace}, BatchId: {BatchId}, Length: {Length}, Created: {Created}, Modified: {Modified}";
}