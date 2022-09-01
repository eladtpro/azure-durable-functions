namespace ImageIngest.Functions.Interfaces;
public interface IDurableStorage
{
    IDictionary<string, ImageMetadata> Get();
    void Upsert(ImageMetadata metadata);
    void UpdateAll(ActivityAction update);
    void CLear();
}
