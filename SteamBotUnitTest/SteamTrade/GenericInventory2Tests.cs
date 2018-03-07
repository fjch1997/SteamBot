using NUnit.Framework;
using SteamTrade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using System.Collections.Specialized;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using SteamBotUnitTest.Properties;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using SteamTrade.Exceptions;

namespace SteamBotUnitTest.SteamTrade
{
    [TestFixture]
    public class GenericInventory2Tests
    {
        [Test]
        public void LoadTest()
        {
            var genericInventory2 = new GenericInventory2(new DelegateFetchSteamWeb(url =>
            {
                if (url == "https://steamcommunity.com/inventory/76561198101672411/570/2?l=schinese&count=5000")
                {
                    using (var reader = new StreamReader(new MemoryStream(Resources.NewApiSampleInventoryPage1)))
                    {
                        return reader.ReadToEndAsync();
                    }
                }
                if (url == "https://steamcommunity.com/inventory/76561198101672411/570/2?l=schinese&count=5000&start_assetid=12942034383")
                {
                    using (var reader = new StreamReader(new MemoryStream(Resources.NewApiSampleInventoryPage2)))
                    {
                        return reader.ReadToEndAsync();
                    }
                }
                if (url == "https://steamcommunity.com/inventory/76561198101672411/570/2?l=schinese&count=5000&start_assetid=8432580706")
                {
                    using (var reader = new StreamReader(new MemoryStream(Resources.NewApiSampleInventoryPage3)))
                    {
                        return reader.ReadToEndAsync();
                    }
                }
                throw new AssertionException("Failed.");
            }), 76561198101672411UL, 570U, 2U, "schinese");
            genericInventory2.Wait();
            Assert.AreEqual(10873, genericInventory2.GetItemCount());

            var item = genericInventory2.GetDescription<ItemDescription>(230751399U, 2748948653U);
            Assert.AreEqual("动能：魔狱霸主！", item.MarketName);
            Assert.AreEqual("Kinetic: Crown of Hells!", item.MarketHashName);

            item = genericInventory2.GetDescription<ItemDescription>(genericInventory2.GetItem(12997738541UL));
            Assert.AreEqual("流浪剑客壁垒", item.MarketName);
            Assert.AreEqual("Bulwark of the Rogue Knight", item.MarketHashName);
            Assert.True(item.Tradable);

            item = genericInventory2.GetDescription<ItemDescription>(12997738541UL);
            Assert.AreEqual("流浪剑客壁垒", item.MarketName);
            Assert.AreEqual("Bulwark of the Rogue Knight", item.MarketHashName);
            Assert.True(item.Tradable);

            item = genericInventory2.GetDescription<ItemDescription>(8611853506UL);
            Assert.AreEqual("战乱天堂", item.MarketName);
            Assert.AreEqual("Wartorn Heavens", item.MarketHashName);
            Assert.False(item.Tradable);

            foreach (var asset in genericInventory2)
            {
                Assert.NotNull(genericInventory2.GetDescription(asset));
            }
        }

        [Test]
        public void SerializationTest()
        {
            var genericInventory2 = new GenericInventory2(new DelegateFetchSteamWeb(() =>
            {
                using (var reader = new StreamReader(new MemoryStream(Resources.SampleInventory)))
                {
                    return reader.ReadToEndAsync();
                }
            }), 76561198058183411UL, 570U, 2U);
            genericInventory2.Wait();

            var serializer = new JsonSerializer() { ContractResolver = new DefaultContractResolver() { IgnoreSerializableAttribute = false } };
            var tempFileName = Path.GetTempFileName();
            using (var stream = File.Create(tempFileName))
            using (var writer = new StreamWriter(stream))
            {
                serializer.Serialize(writer, genericInventory2);
            }
            using (var stream = File.OpenRead(tempFileName))
            using (var streamReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(streamReader))
            {
                genericInventory2 = serializer.Deserialize<GenericInventory2>(reader);
                var item = genericInventory2.GetDescription<ItemDescription>(genericInventory2.GetItem(10918549176UL));
                Assert.AreEqual("冷血刺客副手匕首", item.Name);
                Assert.AreEqual("冷血刺客副手匕首", item.MarketName);
                Assert.AreEqual("Dagger of the Frozen Blood Off-Hand", item.MarketHashName);
            }
            File.Delete(tempFileName);
        }

        [Test]
        public void SerializationExceptionTest()
        {
            var genericInventory2 = new GenericInventory2(new DelegateFetchSteamWeb(async () =>
            {
                await Task.Delay(10000);
                throw new InvalidOperationException();
            }), 76561198058183411UL, 570U, 2U);
            var serializer = new JsonSerializer() { ContractResolver = new DefaultContractResolver() { IgnoreSerializableAttribute = false } };
            var tempFileName = Path.GetTempFileName();
            using (var stream = File.Create(tempFileName))
            using (var writer = new StreamWriter(stream))
            {
                Assert.Throws(typeof(TargetInvocationException), () => serializer.Serialize(writer, genericInventory2));
            }
            File.Delete(tempFileName);
        }

        [Test]
        public void LoadErrorTest()
        {
            var genericInventory2 = new GenericInventory2(new DelegateFetchSteamWeb(() =>
            {
                using (var reader = new StreamReader(new MemoryStream(Resources.inventoryError)))
                {
                    return reader.ReadToEndAsync();
                }
            }), 76561198058183411UL, 570U, 2U);
            var exception = (TradeJsonException)Assert.Throws(typeof(TradeJsonException), () => genericInventory2.Wait());
            Assert.True(exception.Message.Contains("此个人资料是私密的。"));
        }
    }
}
