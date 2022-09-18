namespace ImageIngest.Functions.Interfaces;

public interface IDurableSasToken
{
    SasToken Value { get; set; }
    void Set(SasToken value);
    Task<SasToken> Get();
    Task Reset();
}
