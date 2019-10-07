using Eclipse.Hono.DotNet.DataModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using MQTTnet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Hono.DotNet
{
    public abstract class HonoDeviceClient
    {
        // baseUrl for Adapter
        protected string baseUrl = "unknown";
        // Hono deviceID
        protected string deviceId = "unknown";
        // Hono tenantID
        protected string tenantId = "unknown";
        // Logger
        protected ILogger Logger { get; }

        public HonoDeviceClient(string baseUrl, string deviceId, string tenantId, ILogger logger = null)
        {
            this.baseUrl = baseUrl;
            this.deviceId = deviceId;
            this.tenantId = tenantId;
            if (logger == null)
            {
                this.Logger = new LoggerFactory().CreateLogger(typeof(HonoDeviceClient));
            } else
            {
                this.Logger = logger; 
            }
            
        }

        public abstract Task<SendResult> SendTelemetryAsync(byte[] payload, string contentType);

        public abstract Task<SendResult> SendEventAsync(byte[] eventData, string contentType);

        public abstract Task<SendResult> SendCommandResponseAsync(Command command, int status, byte[] resonse, string contentType);

        public abstract Task<Command> ReceiveCommandAsync();

    }
}
