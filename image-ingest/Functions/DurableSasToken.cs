namespace ImageIngest.Functions;

[JsonObject(MemberSerialization.OptIn)]
public class DurableSasToken : IDurableSasToken
{
    [JsonProperty("value")]
    public SasToken Value { get; set; }
    public void Set(SasToken value) => this.Value = value;
    public Task<SasToken> Get() => Task.FromResult<SasToken>(this.Value);
    public async Task Reset() 
    {
        Value = null;
        await Task.CompletedTask;
    }

    [FunctionName(nameof(DurableSasToken))]

    public static Task Run([EntityTrigger] IDurableEntityContext ctx)
        => ctx.DispatchAsync<DurableSasToken>();
}