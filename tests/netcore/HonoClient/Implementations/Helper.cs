using Eclipse.Hono.DotNet.DataModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace Eclipse.Hono.DotNet.Implementations
{
    public class Helper
    {
        //for Request/Response commands: command///req/${req-id}/${command}[/*][/property-bag]
        //for one - way commands:        command///req//${command}[/*][/property-bag]
        // Property bag /?seqNo=10034&importance="high"
        private static Regex rxmqttcommand = new Regex(@"^command///(req|q)/(?<reqid>[\w\d-]+)?/(?<command>\w+)?([/\w\d-[\?]]+)*(?<propertybag>\?.+)?", ///
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Property bag ?seqNo=10034&importance="high"
        private static Regex rxpropertybag = new Regex(@"^\?((?<keyvalue>[\w\d]+=[\w\d]+)[&]?)*",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Command ParseCommandTopicMQTT(string topic, ILogger logger, bool authenticated = true)
        {
            // Result Object
            var result = new Command();
            result.RequestResponse = false;
            // Find Matches 
            MatchCollection matches = rxmqttcommand.Matches(topic);
            logger.LogDebug($"Parse Command Topic: {matches.Count} matches found in {topic}.");

            foreach (Match match in matches)
            {
                GroupCollection groups = match.Groups;
                logger.LogDebug($"{groups["reqid"].Name} : {groups["reqid"].Value}");
                logger.LogDebug($"{groups["command"].Name} : {groups["command"].Value}");
                logger.LogDebug($"{groups["propertybag"].Name} : {groups["propertybag"].Value}");

                if (groups["reqid"].Success)
                {
                    result.RequestResponse = true;
                    result.RequestId = groups["reqid"].Value;
                }

                if (groups["command"].Success)
                {
                    result.CommandName = groups["command"].Value;
                }

                if (groups["propertybag"].Success)
                {
                    MatchCollection propertybags = rxpropertybag.Matches(groups["propertybag"].Value);
                    foreach (Match bag in propertybags)
                    {
                        var captures = bag.Groups["keyvalue"].Captures;

                        foreach (Capture item in captures)
                        {
                            var keyvalue = item.Value.Split('=');
                            logger.LogDebug($"PropertyBag: {keyvalue[0]} : {keyvalue[1]}");
                            result.PropertyBag.Add(keyvalue[0], keyvalue[1]);
                        }
                    }
                }
            }
            return result;
        }

        public static byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }
}
