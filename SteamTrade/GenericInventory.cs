using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamKit2;
using SteamTrade.TradeWebAPI;
using System.ComponentModel;
using System.Net;
using Newtonsoft.Json.Linq;

namespace SteamTrade
{
    /// <summary>
    /// Generic Steam Backpack Interface.
    /// </summary>
    public class GenericInventory : GenericInventory<ItemDescription>
    {
        public GenericInventory(ISteamWeb steamWeb) : base(steamWeb) { }
    }

    /// <summary>
    /// Generic Steam Backpack Interface. <see cref="T"/> is your own implementation of <see cref="ItemDescription"/>. See documentation on <see cref="ItemDescription"/> for more details.
    /// </summary>
    public class GenericInventory<T> where T : ItemDescription
    {
        private readonly ISteamWeb SteamWeb;

        public GenericInventory(ISteamWeb steamWeb)
        {
            SteamWeb = steamWeb;
        }

        [Obsolete("Use Items instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Dictionary<ulong, Item> items => Items;
        public Dictionary<ulong, Item> Items
        {
            get
            {
                if (_loadTask == null)
                    return null;
                _loadTask.Wait();
                return _items;
            }
        }

        [Obsolete("Use Descriptions instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Dictionary<string, T> descriptions => Descriptions;
        /// <summary>
        /// More details of all <see cref="Items"/>. Key is <see cref="Item.DescriptionId"/>.
        /// </summary>
        public Dictionary<string, T> Descriptions
        {
            get
            {
                if (_loadTask == null)
                    return null;
                _loadTask.Wait();
                return _descriptions;
            }
        }

        [Obsolete("Use Errors instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public List<string> errors => Errors;
        public List<string> Errors
        {
            get
            {
                if (_loadTask == null)
                    return null;
                _loadTask.Wait();
                return _errors;
            }
        }

        [Obsolete("Use IsLoaded instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool isLoaded => IsLoaded;
        public bool IsLoaded { get; private set; } = false;

        private Task _loadTask;
        private Dictionary<string, T> _descriptions = new Dictionary<string, T>();
        private Dictionary<ulong, Item> _items = new Dictionary<ulong, Item>();
        private List<string> _errors = new List<string>();

        /// <summary>
        /// Class containing basic information about an item. For more details, use <see cref="DescriptionId"/> as dictionary key to access <see cref="GenericInventory{T}.Descriptions"/>.
        /// </summary>
        public class Item : TradeUserAssets
        {
            public Item(int appid, long contextid, ulong assetid, string descriptionid, int amount = 1) : base(appid, contextid, assetid, amount)
            {
                this.DescriptionId = descriptionid;
            }

            [Obsolete("Use DescriptionId instead.")]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public string descriptionid => DescriptionId;
            /// <summary>
            /// Use this as dictionary key to access <see cref="GenericInventory{T}.Descriptions"/>.
            /// </summary>
            public string DescriptionId { get; private set; }

            public override string ToString()
            {
                return string.Format("id:{0}, appid:{1}, contextid:{2}, amount:{3}, descriptionid:{4}",
                    assetid, appid, contextid, amount, DescriptionId);
            }
        }

        [Obsolete("Use GetDescription instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ItemDescription getDescription(ulong assetId) { return GetDescription(assetId); }

        /// <summary>
        /// Returns information (such as item name, etc) about the given item.
        /// If no description with the given <param name="assetId"></param> is found, return null.
        /// </summary>
        public T GetDescription(ulong assetId)
        {
            if (_loadTask == null)
                return null;
            _loadTask.Wait();

            Item item;
            if (!_items.TryGetValue(assetId, out item))
                return null;
            T description;
            if (!_descriptions.TryGetValue(item.DescriptionId, out description))
                return null;
            return description;
        }

        [Obsolete("Use Load(int, IEnumerable<long>, SteamID) instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void load(int appid, IEnumerable<long> contextIds, SteamID steamid) { Load(appid, contextIds, steamid); }
        /// <summary>
        /// Load this <see cref="GenericInventory"/>. Results will be stored in <see cref="Items"/> and <see cref="Descriptions"/>. You should check <see cref="Errors"/> for failures. No exception will be thrown.
        /// </summary>
        /// <param name="contextIds">Inspect the network data in the web browser inventory page to determine what context IDs to use.</param>
        /// <param name="steamid">Steam ID of whose inventory is to load.</param>
        /// <param name="maxRetryCountForWebExceptions">Due to the fact that the data to download is relatively huge and Steam's servers are often unstable, a retry is used when sending HTTP requests.</param>
        public void Load(int appid, IEnumerable<long> contextIds, SteamID steamid, int maxRetryCountForWebExceptions = 2)
        {
            List<long> contextIdsCopy = contextIds.ToList();
            _loadTask = Task.Factory.StartNew(() => LoadImplementation(appid, contextIdsCopy, steamid, maxRetryCountForWebExceptions));
        }

        /// <summary>
        /// Load this <see cref="GenericInventory"/> asynchronously. Results will be stored in <see cref="Items"/> and <see cref="Descriptions"/>. You should check <see cref="Errors"/> for failures. No exception will be thrown.
        /// </summary>
        /// <param name="contextIds">Inspect the network data in the web browser inventory page to determine what context IDs to use.</param>
        /// <param name="steamid">Steam ID of whose inventory is to load.</param>
        /// <param name="maxRetryCountForWebExceptions">Due to the fact that the data to download is relatively huge and Steam's servers are often unstable, a retry is used when sending HTTP requests.</param>
        public Task LoadAsync(int appid, IEnumerable<long> contextIds, SteamID steamid, int maxRetryCountForWebExceptions = 2) { Load(appid, contextIds, steamid, maxRetryCountForWebExceptions); return _loadTask; }

        [Obsolete("Use LoadAsync(int, IEnumerable<long>, SteamID, int) and call Task.Wait() instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void loadImplementation(int appid, IEnumerable<long> contextIds, SteamID steamid) { LoadImplementation(appid, contextIds, steamid); }
        /// <summary>
        /// Before overriding this method, be sure to read the source code first.
        /// </summary>
        protected virtual void LoadImplementation(int appid, IEnumerable<long> contextIds, SteamID steamid, int maxRetryCountForWebExceptions = 2)
        {
            dynamic invResponse;
            IsLoaded = false;
            Dictionary<string, string> tmpAppData;

            _items.Clear();
            _descriptions.Clear();
            _errors.Clear();

            try
            {
                foreach (long contextId in contextIds)
                {
                    var start = 0;
                    while (true)
                    {
                        string response;
                        var timesRetried = 0;
                        retry:
                        try
                        {
                            response = SteamWeb.Fetch($"https://steamcommunity.com/profiles/{steamid.ConvertToUInt64()}/inventory/json/{appid}/{contextId}{(start == 0 ? "" : $"?start={start}")}", "GET", null, true);
                        }
                        catch (WebException)
                        {
                            if (timesRetried >= maxRetryCountForWebExceptions)
                                throw;
                            else
                                timesRetried++;
                            goto retry;
                        }
                        invResponse = JsonConvert.DeserializeObject(response);

                        if (invResponse.success == false)
                        {
                            _errors.Add("Fail to open backpack: " + invResponse.Error);
                            continue;
                        }

                        //rgInventory = Items on Steam Inventory 
                        foreach (var item in invResponse.rgInventory)
                        {

                            foreach (var itemId in item)
                            {
                                string descriptionid = itemId.classid + "_" + itemId.instanceid;
                                _items.Add((ulong)itemId.id, new Item(appid, contextId, (ulong)itemId.id, descriptionid));
                                break;
                            }
                        }

                        // rgDescriptions = Item Schema (sort of)
                        foreach (var description in invResponse.rgDescriptions)
                        {
                            foreach (var class_instance in description)// classid + '_' + instenceid 
                            {
                                var descriptionObject = ((JObject)class_instance).ToObject<T>();
                                descriptionObject.Url = (class_instance.actions != null && class_instance.actions.First["link"] != null ? class_instance.actions.First["link"] : "");
                                _descriptions[(string)((class_instance.classid ?? '0') + "_" + (class_instance.instanceid ?? '0'))] = descriptionObject;
                                break;
                            }
                        }

                        //Check for more
                        if ((bool)invResponse.more)
                            start = (int)invResponse.more_start;
                        else
                            break;
                    }
                }//end for (contextId)
            }//end try
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                _errors.Add("Exception: " + e.Message);
            }
            IsLoaded = true;
        }
    }

    /// <summary>
    /// This class is used to deserialize item descriptions from JSON using <see cref="Newtonsoft.Json"/>. You may inherit this class to include more app specific information. Use <see cref="JsonPropertyAttribute"/> on its properties.
    /// For a sample JSON response, hit http://steamcommunity.com/profiles/76561198104350201/inventory/json/570/2
    /// </summary>
    public class ItemDescription
    {
        [JsonProperty("name")]
        public string Name { get; set; }
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
}
