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
using System.Diagnostics;
using Lloyd.Shared.Extensions;

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
        private readonly Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();
        private readonly ItemDescriptionDictionary descriptions = new ItemDescriptionDictionary();
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

        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Initialize a new instance of <see cref="GenericInventory2"/> and loads data.
        /// </summary>
        public GenericInventory2(ISteamWeb steamWeb, ulong steamId64, uint appId, uint contextId, string language = "english")
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
            task?.Wait();
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
            var item = items[assetId];
            if (items.Remove(assetId))
            {
                return descriptions.Remove((item.ClassId, item.InstanceId));
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
            if (items.Remove(item.assetid))
            {
                return descriptions.Remove((item.ClassId, item.InstanceId));
            }
            else
            {
                return false;
            }
        }

        private async Task LoadAsync(int start = 0)
        {
            //Download
            List<JObject> responses = new List<JObject>();

            //Download
            (bool moreItem, ulong startAssetId, int totalItemCount, int counter) result = (true, 0, 0, 1);
            do
            {
                result = await RawDownloadAsync(responses, result.startAssetId, result.counter);
            } while (result.moreItem);

            foreach (var response in responses)
            {
                var assetsJArray = (JArray) response["assets"];
                if (assetsJArray == null)
                    continue; // In RawDownloadAsync we already ensured success: 1. Therefore it must mean that the user has no item in this inventory.
                var assets = assetsJArray.Values<JObject>().ToArray();
                var length = assets.Length;
                var descriptions = (JArray)response.Property("descriptions").Value;
                for (int _i = 0; _i < length; _i++)
                {
                    var item = assets[_i];
                    try
                    {
                        ulong id = 0;
                        var assetId = item.Property("assetid");
                        if (assetId == null || !ulong.TryParse(assetId.Value.ToString(), out id))
                        {
                            if (!long.TryParse(item["currencyid"]?.ToString(), out _))
                            {
                                continue;
                            }
                        }
                        var classId = item["classid"].Value<ulong>();
                        var instanceId = item["instanceid"].Value<ulong>();
                        var amount = item["amount"]?.Value<int>() ?? 1;

                        //Get description
                        JObject description = null;
                        foreach (JObject _description in descriptions)
                        {
                            if (_description.Property("classid").Value.ToString() == classId.ToString() && _description.Property("instanceid").Value.ToString() == instanceId.ToString())
                            {
                                description = _description;
                                break;
                            }
                        }

                        items[id] = new Item(appId, contextId, id, classId, instanceId, amount);
                        this.descriptions[(classId, instanceId)] = description ?? throw new Exception("Description not found.");
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        Warnings.Add("解析物品的过程中失败：" + ex.Message + "。输出的结果可能因此缺少某些项目");
                    }
                }
            }
        }
        private async Task<(bool moreItem, ulong lastAssetId, int totalItemCount, int counter)> RawDownloadAsync(List<JObject> responses, ulong startAssetId = 0, int counter = 1, int maxItemCount = 5000)
        {
            string jsonString;
            var retryCount = 0;
            retry:
            try
            {
                jsonString = await steamWeb.FetchAsync($"https://steamcommunity.com/inventory/{steamId64}/{appId}/{contextId}?l=schinese&count={maxItemCount}" + (startAssetId != 0UL ? $"&start_assetid={startAssetId}" : ""), "GET");
            }
            catch (WebException ex) when (ex.Response == null || ex.Response is HttpWebResponse httpWebResponse && httpWebResponse.StatusCode != HttpStatusCode.Forbidden && httpWebResponse.StatusCode != HttpStatusCode.NotFound)
            {
                if (retryCount < 3)
                {
                    await Task.Delay(3000);
                    retryCount++;
                    goto retry;
                }
                throw;
            }
            var jsonResponse = (JObject)JsonConvert.DeserializeObject(jsonString);
            if (!(bool)jsonResponse.Property("success").Value)
            {
                var errorText = jsonResponse.Property("Error")?.Value.ToString() ?? jsonResponse.Property("error")?.Value.ToString();
                new TradeJsonException("无法获取库存信息：" + errorText, jsonString);
            }
            responses.Add(jsonResponse);
            var moreItemProperty = jsonResponse.Property("more_items");
            var lastAssetIdProperty = jsonResponse.Property("last_assetid");
            return (moreItemProperty != null && (bool)moreItemProperty.Value, lastAssetIdProperty == null ? 0 : (ulong)lastAssetIdProperty.Value,
                (int)jsonResponse.Property("total_inventory_count").Value, counter + 1);
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
        class ItemDescriptionDictionary : Dictionary<(ulong classId, ulong instanceId), JObject> { }
    }
}
