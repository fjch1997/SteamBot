using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamTrade.Exceptions;
using SteamTrade.TradeWebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteamTrade
{
    /// <summary>
    /// An inventory loader and storage for a specific steam ID, appId and contextId. Inventory is loaded asynchronously when this class is created. It becomes immutable once loaded.
    /// </summary>
    /// <remarks>This class is thread-safe.</remarks>
    [Serializable]
    public class GenericInventory2 : GenericInventory2<ItemDescription>
    {
        public GenericInventory2(ISteamWeb steamWeb, ulong steamId64, uint appId, uint contextId) : base(steamWeb, steamId64, appId, contextId)
        {
        }
    }

    /// <summary>
    /// An inventory loader and storage for a specific steam ID, appId and contextId. Inventory is loaded asynchronously when this class is created. It becomes immutable once loaded.
    /// </summary>
    /// <remarks>This class is thread-safe.</remarks>
    [Serializable]
    public class GenericInventory2<T> where T : ItemDescription
    {
        private readonly ISteamWeb steamWeb;
        private readonly ulong steamId64;
        private readonly uint appId;
        private readonly uint contextId;
        private readonly Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();
        private readonly Dictionary<(ulong classId, ulong instanceId), JObject> descriptions = new Dictionary<(ulong classId, ulong instanceId), JObject>();
        [NonSerialized]
        private Task task;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (!Loaded)
                this.task = LoadAsync().ContinueWith(t => { if (t.Exception == null) this.task = null; });
            task.ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a read only dictionary for all <see cref="Item"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Not fully loaded.</exception>
        /// <exception cref="TradeJsonException">Data has been downloaded but an error occurred while parsing it.</exception>
        /// <exception cref="WebException">A network error while connecting to steam servers.</exception>
        public IReadOnlyDictionary<ulong, Item> Items
        {
            get
            {
                if (!Loaded)
                    throw new InvalidOperationException($"This {nameof(GenericInventory2<T>)} has not completed loading yet.");
                Wait();
                return items;
            }
        }

        /// <summary>
        /// Gets a read only dictionary for all descriptions.
        /// </summary>
        /// <exception cref="InvalidOperationException">Not fully loaded.</exception>
        /// <exception cref="TradeJsonException">Data has been downloaded but an error occurred while parsing it.</exception>
        /// <exception cref="WebException">A network error while connecting to steam servers.</exception>
        public IReadOnlyDictionary<(ulong classId, ulong instanceId), JObject> DescriptionsRaw
        {
            get
            {
                if (!Loaded)
                    throw new InvalidOperationException($"This {nameof(GenericInventory2<T>)} has not completed loading yet.");
                Wait();
                return descriptions;
            }
        }

        /// <summary>
        /// If the inventory data has been loaded. After <see cref="Loaded"/> turns true, <see cref="GenericInventory2{T}"/> becomes immutable.
        /// </summary>
        public bool Loaded
        {
            get
            {
                if (task == null)
                    return true;
                else if (task != null && (task.Exception != null || task.IsCompleted))
                    return true;
                else
                    return false;
            }
        }

        public GenericInventory2(ISteamWeb steamWeb, ulong steamId64, uint appId, uint contextId)
        {
            this.steamWeb = steamWeb;
            this.steamId64 = steamId64;
            this.appId = appId;
            this.contextId = contextId;
            this.task = LoadAsync();
            task.ConfigureAwait(false);
        }

        /// <summary>
        /// Wait for the data to be loaded.
        /// </summary>
        /// <exception cref="TradeJsonException">Data has been downloaded but an error occurred while parsing it.</exception>
        /// <exception cref="WebException">A network error while connecting to steam servers.</exception>
        public void Wait()
        {
            if (task != null)
            {
                task.Wait();
                if (task.Exception != null)
                    throw task.Exception;
            }
        }

        /// <summary>
        /// Wait for the data to be loaded.
        /// </summary>
        /// <exception cref="TradeJsonException">Data has been downloaded but an error occurred while parsing it.</exception>
        /// <exception cref="WebException">A network error while connecting to steam servers.</exception>
        public Task WaitAsync()
        {
            return task ?? Task.FromResult(true);
        }

        /// <summary>
        /// Gets a typed description object. Deserialization from raw <see cref="JObject"/> will be performed everytime this is called.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCastException">Serialization failed.</exception>
        /// <exception cref="JsonException">Serialization failed.</exception>
        /// <exception cref="ArgumentException">Serialization failed.</exception>
        public T GetDescription(Item item)
        {
            return descriptions[(item.ClassId, item.InstanceId)].ToObject<T>();
        }

        public T GetDescription(ulong classId, ulong instanceId)
        {
            return descriptions[(classId, instanceId)].ToObject<T>();
        }

        private async Task LoadAsync(int start = 0)
        {
            //Download
            var response = await steamWeb.FetchAsync($"http://steamcommunity.com/profiles/{steamId64}/inventory/json/{appId}/{contextId}{(start == 0 ? "" : $"?start={start}")}", "GET", null, true, "", false);
            JObject jsonObject;
            try
            {
                jsonObject = (JObject)JsonConvert.DeserializeObject(response) ?? throw new TradeJsonException(response);
                //Error message
                var successProperty = jsonObject["success"];
                if (successProperty != null && !successProperty.Value<bool>())
                {
                    throw new TradeJsonException("Failed to parse inventory. " + jsonObject["strError"].Value<string>(), response);
                }

                //Read more
                var moreProperty = jsonObject["more"];
                if (moreProperty != null && moreProperty.Value<bool>())
                {
                    await LoadAsync((int)jsonObject["more_start"].Value<int>());
                }
                else
                {
                    //Parse
                    foreach (JObject item in (jsonObject["rgInventory"]).Children<JProperty>().Select(p => p.Value))
                    {
                        var assetId = item["id"].Value<ulong>();
                        items[assetId] = new Item(appId, contextId, assetId, item["classid"].Value<ulong>(), item["instanceid"].Value<ulong>(), item["amount"]?.Value<int>() ?? 1);
                    }
                    //Parse descirption
                    var rgDescriptions = (JObject)jsonObject["rgDescriptions"];
                    foreach (var item in items)
                    {
                        var key = item.Value.ClassId + "_" + item.Value.InstanceId;
                        descriptions[(item.Value.ClassId, item.Value.InstanceId)] = (JObject)rgDescriptions.Property(key).Value;
                    }
                }
            }
            catch (Exception ex) when (!(ex is WebException) && !(ex is TradeJsonException))
            {
                throw new TradeJsonException("Invalid format.", ex, response);
            }
        }

        /// <summary>
        /// Class containing basic information about an item. For more details, use <see cref="DescriptionId"/> as dictionary key to access <see cref="GenericInventory{T}.Descriptions"/>.
        /// </summary>
        /// <remarks>This class is immutable.</remarks>
        public class Item : TradeUserAssets
        {
            public Item(uint appid, uint contextid, ulong assetid, ulong classId, ulong instanceId, int amount = 1) : base((int)appid, contextid, assetid, amount)
            {
                ClassId = classId;
                InstanceId = instanceId;
            }

            public ulong ClassId { get; private set; }
            public ulong InstanceId { get; private set; }

            public override string ToString()
            {
                return $"id:{assetid}, appid:{appid}, contextid:{contextid}, amount:{amount}, classid:{ClassId}, instanceid: {InstanceId}";
            }
        }
    }
}
