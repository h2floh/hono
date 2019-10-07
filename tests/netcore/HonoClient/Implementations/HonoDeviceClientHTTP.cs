using System;
using System.Threading.Tasks;
using Eclipse.Hono.DotNet.DataModels;

namespace Eclipse.Hono.DotNet
{
    public class HonoDeviceClientHTTP : HonoDeviceClient
    {
        public HonoDeviceClientHTTP(string baseUrl, string deviceId, string tenantId) : base(baseUrl, deviceId, tenantId) => throw new NotImplementedException();

        public override Task<Command> ReceiveCommandAsync()
        {
            throw new NotImplementedException();
        }

        public override Task<SendResult> SendCommandResponseAsync(Command command, int status, byte[] resonse, string contentType)
        {
            throw new NotImplementedException();
        }

        public override Task<SendResult> SendEventAsync(byte[] eventData, string contentType)
        {
            throw new NotImplementedException();
        }

        public override Task<SendResult> SendTelemetryAsync(byte[] payload, string contentType)
        {
            throw new NotImplementedException();
        }
    }
}
