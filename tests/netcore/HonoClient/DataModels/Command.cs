using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Eclipse.Hono.DotNet.DataModels
{
    public class Command
    {
        public string CommandName { get; set; }
        public string Status { get; set;  }
        public string RequestId { get; set; }
        public Dictionary<string, string> PropertyBag { get; private set; }
        public bool RequestResponse { get; set; }
        public byte[] Payload { get; set; }

        public Command()
        {
            PropertyBag = new Dictionary<string, string>();
        }
    }
}
