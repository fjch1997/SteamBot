using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SteamTrade.Exceptions;
using SteamTrade.TradeWebAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

namespace SteamTrade
{
    /// <summary>
    /// An inventory loader and storage for a specific steam ID, appId and contextId. Inventory is loaded asynchronously when this class is created. It becomes immutable once loaded.<para />
    /// This class is thread-safe. <para />
    /// This class is <see cref="Newtonsoft.Json"/> serializable when <see cref="DefaultContractResolver.IgnoreSerializableAttribute"/> is set to false.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.Fields)]
    public class GenericInventory2 : IEnumerable<GenericInventory2.Item>
    {
        [JsonIgnore]
        private readonly ISteamWeb steamWeb;
        private readonly ulong steamId64;
        private readonly uint appId;
        private readonly uint contextId;
        private readonly ConcurrentDictionary<ulong, Item> items = new ConcurrentDictionary<ulong, Item>();
        private readonly ItemDescriptionConcurrentDictionary descriptions = new ItemDescriptionConcurrentDictionary();
        [JsonIgnore]
        private Task task;

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (!Loaded)
                throw new InvalidOperationException(nameof(GenericInventory2) + " must be loaded before it can be serialized.");
        }

        /// <summary>
        /// If the inventory data has been loaded. After <see cref="Loaded"/> turns true, <see cref="GenericInventory2"/> becomes immutable.
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

        /// <summary>
        /// appid.
        /// </summary>
        public uint AppId => appId;
        /// <summary>
        /// contextid.
        /// </summary>
        public uint ContextId => contextId;

        /// <summary>
        /// Initialize a new instance of <see cref="GenericInventory2"/> and loads data.
        /// </summary>
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
        /// Wait for the data to be loaded. To reload a <see cref="GenericInventory2"/>, create another instance.
        /// </summary>
        /// <exception cref="TradeJsonException">Data has been downloaded but an error occurred while parsing it.</exception>
        /// <exception cref="WebException">A network error while connecting to steam servers.</exception>
        public void Wait()
        {
            if (task != null)
            {
                try
                {
                    task.Wait();
                }
                catch(AggregateException ex) when (ex.InnerExceptions.Count == 1)
                {
                    throw ex.InnerException;
                }
            }
        }

        /// <summary>
        /// Wait for the data to be loaded. To reload a <see cref="GenericInventory2"/>, create another instance.
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
        public T GetDescription<T>(Item item)
        {
            return descriptions[(item.ClassId, item.InstanceId)].ToObject<T>();
        }

        /// <summary>
        /// Gets a typed description object. Deserialization from raw <see cref="JObject"/> will be performed everytime this is called.
        /// </summary>
        /// <exception cref="InvalidCastException">Serialization failed.</exception>
        /// <exception cref="JsonException">Serialization failed.</exception>
        /// <exception cref="ArgumentException">Serialization failed.</exception>
        public T GetDescription<T>(ulong classId, ulong instanceId)
        {
            return descriptions[(classId, instanceId)].ToObject<T>();
        }

        /// <summary>
        /// Gets a an item by <see cref="TradeUserAssets.assetid"/>.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The <paramref name="assetId"/> does not exist.</exception>
        /// <exception cref="InvalidCastException">Serialization failed.</exception>
        /// <exception cref="JsonException">Serialization failed.</exception>
        /// <exception cref="ArgumentException">Serialization failed.</exception>
        public T GetDescription<T>(ulong assetId)
        {
            return GetDescription<T>(items[assetId]);
        }

        /// <summary>
        /// Gets the raw description for the <paramref name="item"/>.
        /// </summary>
        /// <returns>Raw <see cref="JObject"/> returned from the server.</returns>
        /// <exception cref="KeyNotFoundException">The <paramref name="item"/> does not exist.</exception>
        public JObject GetDescription(Item item)
        {
            return descriptions[(item.ClassId, item.InstanceId)];
        }

        /// <summary>
        /// Gets the raw description with the given <paramref name="classId"/> and <paramref name="instanceId"/>.
        /// </summary>
        /// <returns>Raw <see cref="JObject"/> returned from the server.</returns>
        /// <exception cref="KeyNotFoundException">An item with the given <paramref name="classId"/> and <paramref name="instanceId"/> does not exist.</exception>
        public JObject GetDescription(ulong classId, ulong instanceId)
        {
            return descriptions[(classId, instanceId)];
        }

        /// <summary>
        /// Gets a an item by <see cref="TradeUserAssets.assetid"/>.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The <paramref name="assetId"/> does not exist.</exception>
        public JObject GetDescription(ulong assetId)
        {
            return GetDescription(items[assetId]);
        }

        /// <summary>
        /// Gets a an item by <see cref="TradeUserAssets.assetid"/>.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The <paramref name="assetId"/> does not exist.</exception>
        public Item GetItem(ulong assetId)
        {
            return items[assetId];
        }

        /// <summary>
        /// Gets the number of item this 
        /// </summary>
        /// <returns></returns>
        public int GetItemCount()
        {
            return items.Count;
        }

        /// <summary>
        /// Removes an item with the given <paramref name="assetId"/>. 
        /// This can avoid downloading the lastest inventory data when an item has been traded out of the inventory but nothing was received in return.
        /// </summary>
        /// <param name="assetId"><see cref="TradeUserAssets.assetid"/>.</param>
        /// <returns>true if the item and its description was removed successfully; otherwise, false.</returns>
        public bool RemoveItem(ulong assetId)
        {
            if (items.TryRemove(assetId, out Item item))
            {
                return descriptions.TryRemove((item.ClassId, item.InstanceId), out JObject description);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Removes an item with the given <paramref name="item"/>. 
        /// This can avoid downloading the lastest inventory data when an item has been traded out of the inventory but nothing was received in return.
        /// </summary>
        /// <returns>true if the item and its description was removed successfully; otherwise, false.</returns>
        public bool RemoveItem(Item item)
        {
            if (items.TryRemove(item.assetid, out Item _item))
            {
                return descriptions.TryRemove((_item.ClassId, _item.InstanceId), out JObject description);
            }
            else
            {
                return false;
            }
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
                    var strError = jsonObject["strError"];
                    if (strError != null)
                        throw new TradeJsonException("Failed to parse inventory. " + strError.Value<string>(), response);
                    var error = jsonObject["Error"];
                    if (error != null)
                        throw new TradeJsonException("Failed to parse inventory. " + error.Value<string>(), response);
                    throw new TradeJsonException("Invalid format.", response);
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
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{T}"/> that can be used to iterate through the collection.</returns>
        public IEnumerator<Item> GetEnumerator()
        {
            return items.Select(i => i.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.Select(i => i.Value).GetEnumerator();
        }

        /// <summary>
        /// Class containing basic information about an item.
        /// </summary>
        /// <remarks>This class is immutable.</remarks>
        [JsonObject(MemberSerialization = MemberSerialization.Fields)]
        public class Item : TradeUserAssets
        {
            /// <summary>
            /// Initailize a new instance of <see cref="Item"/>.
            /// </summary>
            public Item(uint appid, uint contextid, ulong assetid, ulong classId, ulong instanceId, int amount = 1) : base((int)appid, contextid, assetid, amount)
            {
                ClassId = classId;
                InstanceId = instanceId;
            }

            public ulong ClassId { get; private set; }
            public ulong InstanceId { get; private set; }

            /// <summary>
            /// Gets a string representation of all properties.
            /// </summary>
            public override string ToString()
            {
                return $"id:{assetid}, appid:{appid}, contextid:{contextid}, amount:{amount}, classid:{ClassId}, instanceid: {InstanceId}";
            }
        }

        [JsonArray]
        class ItemDescriptionConcurrentDictionary : ConcurrentDictionary<(ulong classId, ulong instanceId), JObject> { }
    }
}
