namespace ImageIngest.Functions.Model;
public class BlobTags
{
    private IDictionary<string, string> tags = new Dictionary<string, string>();
    public IDictionary<string, string> Tags => tags;
    public BlobTags() { }
    public long Created
    {
        get => (long.TryParse(tags[nameof(Created)], out long created) ? created : default(long));
        set => tags[nameof(Created)] = value.ToString();
    }

    public long Modified
    {
        get => (long.TryParse(tags[nameof(Modified)], out long modified) ? modified : default(long));
        set => tags[nameof(Modified)] = value.ToString();
    }

    public BlobStatus Status
    {
        get => (Enum.TryParse<BlobStatus>(tags[nameof(Status)], true, out BlobStatus status) ? status : BlobStatus.New);
        set => tags[nameof(Length)] = value.ToString();
    }

    public string Container
    {
        get => tags.TryGetValue(nameof(Container), out string container) ? container : string.Empty;
        set => tags[nameof(Container)] = value;
    }

    public string Namespace
    {
        get => tags.TryGetValue(nameof(Namespace), out string @namespace) ? @namespace : string.Empty;
        set => tags[nameof(Namespace)] = value;
    }

    public string BatchId
    {
        get => tags.TryGetValue(nameof(BatchId), out string batchId) ? batchId : string.Empty;
        set => tags[nameof(BatchId)] = value;
    }
    public long Length
    {
        get => (long.TryParse(tags[nameof(Length)], out long length) ? length : default(long));
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
    }

    public override string ToString() =>
        $"Namespace: {Namespace}, BatchId: {BatchId}, Length: {Length}, Created: {Created}, Modified: {Modified}";
}