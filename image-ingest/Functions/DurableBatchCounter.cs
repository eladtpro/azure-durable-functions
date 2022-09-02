using Newtonsoft.Json;

namespace ImageIngest.Functions;

[JsonObject(MemberSerialization.OptIn)]
public class DurableBatchCounter : IDurableBatchCounter
{
    [JsonProperty("value")]
    public long Value { get; set; }

    public void Enlist() => Value++;

    public async Task Reset() 
    {
        Value = 0;
        await Task.CompletedTask;
    }
    public void Delete() 
    {
        Entity.Current.DeleteState();
    }

    [FunctionName(nameof(DurableBatchCounter))]

    public static Task Run([EntityTrigger] IDurableEntityContext ctx)
        => ctx.DispatchAsync<DurableBatchCounter>();
}