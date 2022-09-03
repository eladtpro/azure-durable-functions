namespace ImageIngest.Functions.Enums;
[Flags]
public enum BlobStatus
{
    New = 0,
    Pending = 1,
    Batched = 2,
    Zipped = 4,
    Error = 1024,
    Completed = Zipped & Error
}
