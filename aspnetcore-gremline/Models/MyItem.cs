using Newtonsoft.Json;

namespace aspnetcore_gremline.Models
{
    public class MyItem
    {
        [JsonProperty("itemId")]
        public string ItemId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
