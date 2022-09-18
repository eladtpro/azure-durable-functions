using Newtonsoft.Json;

namespace ImageIngest.Functions.Model;

[JsonObject(MemberSerialization.OptIn)]
public class SasToken {
    [JsonProperty("url")]
    public Uri Url { get; set; }
    [JsonProperty("expireson")]
    public DateTimeOffset ExpiresOn { get; set; }

    public override string ToString()
    {
        return $"Url: {Url}, ExpiresOn: {ExpiresOn}";
    }
}