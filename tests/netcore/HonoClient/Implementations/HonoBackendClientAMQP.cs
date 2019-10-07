using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace Eclipse.Hono.DotNet.Implementations
{
    public class HonoBackendClientAMQP : HonoBackendClient
    {
        // Documentation about AMQPNetLite http://azure.github.io/amqpnetlite/articles/hello_amqp.html
        private SenderLink senderLinkCommand;
        private ReceiverLink receiverLinkTwoWayCommand;
        private ReceiverLink receiverLinkEvent;
        private ReceiverLink receiverLinkTelemetry;
        private Address address;
        private Session session;
        private Connection connection;
        private string replyId = Guid.NewGuid().ToString();

        private const string targetAddressCommand = @"command/{0}"; //command/${tenant_id}
        private const string sourceAddressCommandResponse = @"command_response/{0}/{1}"; //command_response/${tenant_id}/${reply_id}
        private const string targetAddressToDevice = @"command/{0}/{1}"; //command/${tenant_id}/${device_id}
        private const string sourceAddressEvent = @"event/{0}"; //event/${tenant_id}
        private const string sourceAddressTelemetry = @"telemetry/{0}"; //telemetry/${tenant_id};

        public HonoBackendClientAMQP(string connectionString, string tenantId, ILogger logger = null) : base(tenantId, logger)
        {
            address = new Address(connectionString);
            connection = new Connection(address);
            session = new Session(connection);
            senderLinkCommand = new SenderLink(session, Guid.NewGuid().ToString(), String.Format(HonoBackendClientAMQP.targetAddressCommand, tenantId));
            receiverLinkEvent = new ReceiverLink(session, Guid.NewGuid().ToString(), String.Format(HonoBackendClientAMQP.sourceAddressEvent, tenantId));
            receiverLinkTelemetry = new ReceiverLink(session, Guid.NewGuid().ToString(), String.Format(HonoBackendClientAMQP.sourceAddressTelemetry, tenantId));
            receiverLinkTwoWayCommand = new ReceiverLink(session, replyId, String.Format(HonoBackendClientAMQP.sourceAddressCommandResponse, tenantId, replyId));
        }

        public async Task SendCommandOneWayAsync(string deviceId, string command, byte[] payload, string contentType = "text/plain; charset=\"utf-8\"")
        {
            Message message = new Message() { BodySection = new Data() { Binary = payload } };
            message.Properties = new Amqp.Framing.Properties();
            message.Properties.To = String.Format(HonoBackendClientAMQP.targetAddressToDevice, tenantId, deviceId);
            message.Properties.Subject = command;
            message.Properties.ContentType = contentType;
            message.Properties.SetMessageId(Guid.NewGuid().ToString());

            Logger.LogDebug($"Send one way command '{command}' to device '{deviceId}'");
            try
            {
                await senderLinkCommand.SendAsync(message);
            }
            catch (Exception e)
            {
                Logger.LogDebug($"{e.Message}");
            }
            
        }

        public async Task SendCommandTwoWayAsync(string deviceId, string command, byte[] payload, string contentType = "text/plain; charset=\"utf-8\"")
        {
            Message message = new Message() { BodySection = new Data() { Binary = payload } };
            message.Properties = new Amqp.Framing.Properties();
            message.Properties.To = String.Format(HonoBackendClientAMQP.targetAddressToDevice, tenantId, deviceId);
            message.Properties.Subject = command;
            message.Properties.ReplyTo = String.Format(HonoBackendClientAMQP.sourceAddressCommandResponse, tenantId, replyId);
            message.Properties.ContentType = contentType;
            message.Properties.SetMessageId("");
            message.Properties.SetCorrelationId("");
            message.Properties.CreationTime = DateTime.UtcNow;
            //message.Properties.AbsoluteExpiryTime = DateTime.MaxValue;

            try
            {
                Logger.LogDebug($"Send req/res command '{command}' to device '{deviceId}'");
                await senderLinkCommand.SendAsync(message);
            }
            catch (Exception e)
            {
                Logger.LogDebug($"{e.Message}");
            }

        }

        public async Task<Message> ReceiveCommandResponseAsync()
        {
            try
            {
                var response = await receiverLinkTwoWayCommand.ReceiveAsync();
                
                Data data = (Data)response.BodySection;
                Logger.LogDebug($"Recieved response from device: '{response.ApplicationProperties["device_id"]}', status: '{response.ApplicationProperties["status"]}' payload: '{Encoding.UTF8.GetString(data.Binary)}'");
                
                receiverLinkTwoWayCommand.Accept(response);

                return response;
            }
            catch (Exception e)
            {
                Logger.LogDebug($"{e.Message}");
            }
            return null;
        }

        public async Task<Message> ReceiveEventAsync()
        {
            Message response = null;
            try
            {
                response = await receiverLinkEvent.ReceiveAsync();
                receiverLinkEvent.Accept(response);

                if (response.BodySection != null)
                {
                    Data data = (Data)response.BodySection;
                    Logger.LogDebug($"Recieved event from device: {response.ApplicationProperties["device_id"]}, payload: {Encoding.UTF8.GetString(data.Binary)}");
                }
            }
            catch (Exception e)
            {
                Logger.LogDebug($"{e.Message}");
            }
            return response;
        }

        public async Task<Message> ReceiveTelemetryAsync()
        {
            Message response = null;
            try
            {
                response = await receiverLinkTelemetry.ReceiveAsync();
                receiverLinkTelemetry.Accept(response);
                
                if (response.BodySection != null)
                {
                    Data data = (Data)response.BodySection;
                    Logger.LogDebug($"Recieved telemetry from device: {response.ApplicationProperties["device_id"]}, payload: {Encoding.UTF8.GetString(data.Binary)}");
                }
            }
            catch (Exception e)
            {
                Logger.LogDebug($"{e.Message}");
            }
            return response;
        }

        ~HonoBackendClientAMQP()
        {
            senderLinkCommand.Close();
            receiverLinkTwoWayCommand.Close();
            receiverLinkEvent.Close();
            receiverLinkTelemetry.Close();
            session.Close();
            connection.Close();
        }
    }
}
