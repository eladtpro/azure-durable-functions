namespace ImageIngest.Functions.Interfaces;

public interface IDurableBatchCounter
{
    long Value { get; set; }
    void Enlist();
    Task Reset();
    void Delete();
}
