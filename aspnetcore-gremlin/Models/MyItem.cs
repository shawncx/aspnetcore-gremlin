using Newtonsoft.Json;

namespace aspnetcore_gremlin.Models
{
    public class MyItem
    {
        [JsonProperty("itemId")]
        public string ItemId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
