using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eclipse.Hono.DotNet.DataModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Server;

namespace Eclipse.Hono.DotNet
{
    public class HonoDeviceClientMQTT : HonoDeviceClient
    {
        // Const
        private const string unauthenticatedTelemetryTopic = @"telemetry/{0}/{1}";
        private const string authenticatedTelemetryTopic = @"telemetry";
        private const string unauthenticatedEventTopic = @"event/{0}/{1}";
        private const string authenticatedEventTopic = @"event";
        private const string unauthenticatedCommandTopic = @"command/{0}/{1}/req/#";
        private const string authenticatedCommandTopic = @"command/+/+/req/#";
        private const string unauthenticatedCommandResponseTopic = @"command/{0}/{1}/res/{2}/{3}";
        private const string authenticatedCommandResponseTopic = @"command///res/{0}/{1}";
        // MQTT Client
        private MQTTnet.Client.IMqttClient client;
        private MQTTnet.Client.Options.IMqttClientOptions clientOptions;
        private MQTTnet.Protocol.MqttQualityOfServiceLevel serviceLevel;
        private uint MQTTmessageExperyInterval = 6000;
        // Other Status flags
        private bool IsAuthenticated { get; }
        // In Memory Msg Queue
        private LinkedList<Command> cachedCommands = new LinkedList<Command>();

        public HonoDeviceClientMQTT(string baseUrl, string deviceId, string tenantId, string password = null, ILogger logger = null, uint messageExperyInterval = 6000) : base(baseUrl, deviceId, tenantId, logger)
        {          
            // Create a new MQTT client.
            var factory = new MQTTnet.MqttFactory();
            this.client = factory.CreateMqttClient();
            // Create TCP based options using the builder.
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(deviceId)
                .WithTls(new MqttClientOptionsBuilderTlsParameters() { AllowUntrustedCertificates = true })
                .WithTcpServer(baseUrl);

            if (password != null)
            {
                optionsBuilder.WithCredentials($"{deviceId}@{tenantId}", password);
                IsAuthenticated = true;
            }

            // Create TCP based options using the builder.
            this.clientOptions = optionsBuilder
                .Build();

            // Set default QoS 
            // At most once (0)
            // At least once(1)
            // Exactly once(2).
            serviceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce;
            this.MQTTmessageExperyInterval = messageExperyInterval;

            ConnectAndSubscribe().Wait();
        }

        private async Task ConnectAndSubscribe()
        {
            Logger.LogInformation("Connecting to MQTT Broker...");

            await client.ConnectAsync(clientOptions, CancellationToken.None);

            Logger.LogInformation("... Connected.");

            client.UseDisconnectedHandler(async e => { await Reconnect(); });

            client.UseConnectedHandler(async e => { await Subscribe(); });
            await Subscribe();

            client.UseApplicationMessageReceivedHandler(e => { ReceiveMessage(e); });
        }

        private async Task Reconnect()
        {
            Logger.LogError("### DISCONNECTED FROM SERVER ###");
            await Task.Delay(TimeSpan.FromSeconds(5));

            try
            {
                await client.ConnectAsync(clientOptions, CancellationToken.None); // Since 3.0.5 with CancellationToken
            }
            catch
            {
                Logger.LogError("### RECONNECTING FAILED ###");
            }
        }

        private async Task Subscribe()
        {
            Logger.LogInformation("Subscribing to Command Topic...");
            string commandTopic;
            
            if (IsAuthenticated)
            {
                commandTopic = HonoDeviceClientMQTT.authenticatedCommandTopic;
            }
            else
            {
                commandTopic = String.Format(HonoDeviceClientMQTT.unauthenticatedCommandTopic, this.tenantId, this.deviceId);
            }

            await client.SubscribeAsync(new TopicFilterBuilder()
                .WithTopic(commandTopic)
                .WithQualityOfServiceLevel(serviceLevel)
                .Build()
            );
        }

        private void ReceiveMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            Logger.LogDebug($"Received Message Topic: {e.ApplicationMessage.Topic}");
            
            // Parse topic for 
            Command newCommand = Implementations.Helper.ParseCommandTopicMQTT(e.ApplicationMessage.Topic, Logger, IsAuthenticated);
            newCommand.Payload = e.ApplicationMessage.Payload;
            cachedCommands.AddLast(newCommand);
        }

        public async override Task<Command> ReceiveCommandAsync()
        {
            while (cachedCommands.Count == 0)
            {
                await Task.Delay(10);
            }
            
            var result = cachedCommands.First.Value;
            cachedCommands.RemoveFirst();
            
            return result;
        }

        public async override Task<SendResult> SendCommandResponseAsync(Command command, int status, byte[] response, string contentType = "text/plain; charset=\"utf-8\"")
        {
            string commandResponseTopic;

            if (IsAuthenticated)
            {
                commandResponseTopic = String.Format(HonoDeviceClientMQTT.authenticatedCommandResponseTopic, command.RequestId, status);
            }
            else
            {
                commandResponseTopic = String.Format(HonoDeviceClientMQTT.unauthenticatedCommandResponseTopic, this.tenantId, this.deviceId, command.RequestId, status);
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(commandResponseTopic)
                .WithPayload(response)
                .WithContentType(contentType)
                .WithRetainFlag(false)
                .WithMessageExpiryInterval(MQTTmessageExperyInterval)
                .WithAtMostOnceQoS()
                //.WithQualityOfServiceLevel(serviceLevel)
                .Build();

            var result = await client.PublishAsync(message);

            return new SendResult((int)result.ReasonCode, result.ReasonCode.ToString(), result.ReasonString);
        }

        public async override Task<SendResult> SendEventAsync(byte[] eventData, string contentType = "text/plain; charset=\"utf-8\"")
        {
            string eventTopic;

            if (IsAuthenticated)
            {
                eventTopic = HonoDeviceClientMQTT.authenticatedEventTopic;
            }
            else
            {
                eventTopic = String.Format(HonoDeviceClientMQTT.unauthenticatedEventTopic, this.tenantId, this.deviceId);
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(eventTopic)
                .WithPayload(eventData)
                .WithContentType(contentType)
                .WithAtLeastOnceQoS() // Hono Event is At least once (1) mandatory
                .Build();

            Logger.LogDebug($"Send event with payload '{Encoding.UTF8.GetString(eventData)}'");
            var result = await client.PublishAsync(message);

            return new SendResult((int) result.ReasonCode, result.ReasonCode.ToString(), result.ReasonString);
        }

        public async override Task<SendResult> SendTelemetryAsync(byte[] payload, string contentType = "text/plain; charset=\"utf-8\"")
        {
            string telemetryTopic;

            if (IsAuthenticated)
            {
                telemetryTopic = HonoDeviceClientMQTT.authenticatedTelemetryTopic;
            }
            else
            {
                telemetryTopic = String.Format(HonoDeviceClientMQTT.unauthenticatedTelemetryTopic, this.tenantId, this.deviceId);
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(telemetryTopic)
                .WithPayload(payload)
                .WithContentType(contentType)
                .WithRetainFlag(false)
                .WithMessageExpiryInterval(MQTTmessageExperyInterval)
                .WithAtMostOnceQoS()
                //.WithPayloadFormatIndicator(MQTTnet.Protocol.MqttPayloadFormatIndicator.CharacterData)
                //.WithQualityOfServiceLevel(serviceLevel)
                .Build();

            Logger.LogDebug($"Send telemetry with payload '{Encoding.UTF8.GetString(payload)}'");
            var result = await client.PublishAsync(message);

            return new SendResult((int)result.ReasonCode, result.ReasonCode.ToString(), result.ReasonString);
        }
    }
}
