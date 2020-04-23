using System.Collections.Generic;
using Newtonsoft.Json;

namespace SteamTrade
{
    /// <summary>
    /// This class is used to deserialize item descriptions from JSON using <see cref="Newtonsoft.Json"/>. You may inherit this class to include more app specific information. Use <see cref="JsonPropertyAttribute"/> on its properties.
    /// For a sample JSON response, hit http://steamcommunity.com/profiles/76561198104350201/inventory/json/570/2
    /// </summary>
    public class ItemDescription
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("tags")]
        public List<Tag> Tags { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("tradable")]
        public bool Tradable { get; set; }
        [JsonProperty("marketable")]
        public bool Marketable { get; set; }
        public string Url { get; set; }
        [JsonProperty("classid")]
        public long ClassId { get; set; }

        [JsonProperty("appid")]
        public int AppId { get; set; }
        [JsonProperty("instanceid")]
        public long InstanceId { get; set; }
        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }
        [JsonProperty("icon_url_large")]
        public string IconUrlLarge { get; set; }
        [JsonProperty("icon_drag_url")]
        public string IconDragUrl { get; set; }
        [JsonProperty("market_hash_name")]
        public string MarketHashName { get; set; }
        [JsonProperty("market_name")]
        public string MarketName { get; set; }
        [JsonProperty("name_color")]
        public string NameColor { get; set; }
        [JsonProperty("background_color")]
        public string BackgroundColor { get; set; }
        [JsonProperty("commodity")]
        public string Commodity { get; set; }
        [JsonProperty("market_tradable_restriction")]
        public string MarketTradableRestriction { get; set; }
        [JsonProperty("market_marketable_restriction")]
        public string MarketMarketableRestriction { get; set; }
    }
    public class Tag
    {
        [JsonProperty("category")]
        public string Category { get; set; }
        [JsonProperty("internal_name")]
        public string InternalName { get; set; }
        [JsonProperty("localized_category_name")]
        public string LocalizedCategoryName { get; set; }
        [JsonProperty("localized_tag_name")]
        public string LocalizedTagName { get; set; }
    }
}