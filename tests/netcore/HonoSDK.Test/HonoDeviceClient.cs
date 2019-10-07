using NUnit.Framework;
using NUnit.Framework.Internal;
using Eclipse.Hono.DotNet.Implementations;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Eclipse.Hono.DotNet;
using Eclipse.Hono.DotNet.DataModels;

namespace HonoSDK.Test
{
    public class Tests
    {
        Microsoft.Extensions.Logging.ILogger logger;

        [SetUp]
        public void Setup()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug)
                       .AddConsole();
            });
            logger = loggerFactory.CreateLogger("HonoSDKTest");
        }

        [Test]
        public void ParseCommandTopicMQTT()
        {
            var commandIs = Helper.ParseCommandTopicMQTT("command///q/1010f8ab0b53-bd96-4d99-9d9c-56b868474a6a/setBrightness", logger, true);
            Assert.AreEqual(commandIs.RequestResponse, true);
            Assert.AreEqual(commandIs.RequestId, "1010f8ab0b53-bd96-4d99-9d9c-56b868474a6a");
            Assert.AreEqual(commandIs.CommandName, "setBrightness");

            commandIs = Helper.ParseCommandTopicMQTT("command///q//setBrightness", logger, true);
            Assert.AreEqual(commandIs.RequestResponse, false);
            Assert.AreEqual(commandIs.RequestId, null);
            Assert.AreEqual(commandIs.CommandName, "setBrightness");

            commandIs = Helper.ParseCommandTopicMQTT("command///req/1010f8ab0b53-bd96-4d99-9d9c-56b868474a6a/setBrightness", logger, true);
            Assert.AreEqual(commandIs.RequestResponse, true);
            Assert.AreEqual(commandIs.RequestId, "1010f8ab0b53-bd96-4d99-9d9c-56b868474a6a");
            Assert.AreEqual(commandIs.CommandName, "setBrightness");

            commandIs = Helper.ParseCommandTopicMQTT("command///req//setBrightness", logger, true);
            Assert.AreEqual(commandIs.RequestResponse, false);
            Assert.AreEqual(commandIs.RequestId, null);
            Assert.AreEqual(commandIs.CommandName, "setBrightness");

            commandIs = Helper.ParseCommandTopicMQTT("command///q/1010f8ab0b53-bd96-4d99-9d9c-56b868474a6a/setBrightness/asdf/werwer/?abc=100&xyz=Stuttgart", logger, true);
            Assert.AreEqual(commandIs.RequestResponse, true);
            Assert.AreEqual(commandIs.RequestId, "1010f8ab0b53-bd96-4d99-9d9c-56b868474a6a");
            Assert.AreEqual(commandIs.CommandName, "setBrightness");
            Assert.AreEqual(commandIs.PropertyBag["abc"], "100");
            Assert.AreEqual(commandIs.PropertyBag["xyz"], "Stuttgart");

            commandIs = Helper.ParseCommandTopicMQTT("command///q//setBrightness/", logger, true);
            Assert.AreEqual(commandIs.RequestResponse, false);
            Assert.AreEqual(commandIs.RequestId, null);
            Assert.AreEqual(commandIs.CommandName, "setBrightness");

            commandIs = Helper.ParseCommandTopicMQTT("command///q//setBrightness?abc=100&xyz=435", logger, true);
            Assert.AreEqual(commandIs.RequestResponse, false);
            Assert.AreEqual(commandIs.RequestId, null);
            Assert.AreEqual(commandIs.CommandName, "setBrightness");
            Assert.AreEqual(commandIs.PropertyBag["abc"], "100");
            Assert.AreEqual(commandIs.PropertyBag["xyz"], "435");


        }

    }

}