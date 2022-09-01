namespace ImageIngest.Functions.Interfaces;
public interface IDurableStorage
{
    void Upsert(ImageMetadata metadata);
    void UpdateAll(ActivityAction update);
    void CLear();
}
