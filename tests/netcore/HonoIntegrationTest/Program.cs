using Amqp;
using Amqp.Framing;
using Amqp.Types;
using Eclipse.Hono.DotNet;
using Eclipse.Hono.DotNet.Implementations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HonoIntegrationTest
{
    class Program
    {
        // Settings
        static string deviceId = Environment.GetEnvironmentVariable("deviceId");
        static string deviceIdPassword = Environment.GetEnvironmentVariable("deviceIdPassword");
        static string tenantId = Environment.GetEnvironmentVariable("tenantId");
        static string mqttendpoint = Environment.GetEnvironmentVariable("mqttendpoint");
        static string amqpendpoint = Environment.GetEnvironmentVariable("amqpendpoint");
        const int retries = 5;
        // Log Builder
        static ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug)
                   .AddConsole();
        });

        static void Main(string[] args)
        {
            // Logger for Device View
            var loggerDevice = loggerFactory.CreateLogger("Device");
            // Logger for Backend View
            var loggerBackend = loggerFactory.CreateLogger("Backend");
            // Logger for IntegrationTest View
            var loggerTest = loggerFactory.CreateLogger("Test");
            // Cancellation Token
            var cts = new CancellationTokenSource();
            // HONO Device Client
            var device = new HonoDeviceClientMQTT(mqttendpoint, deviceId, tenantId, deviceIdPassword, logger: loggerDevice);
            // HONO Backend Client
            var backend = new HonoBackendClientAMQP(amqpendpoint, tenantId, loggerBackend);
            // Test Results
            LinkedList<TestResult> IntegrationTestResult = new LinkedList<TestResult>();
            var OWCommandTest = new TestResult { Name = "OneWay Command", AmountFail = 0, AmountSuccess = 0 };
            var TWCommandTest = new TestResult { Name = "RequestResponse Command", AmountFail = 0, AmountSuccess = 0 };
            var EventTest = new TestResult { Name = "Event", AmountFail = 0, AmountSuccess = 0 };
            var TelemetryTest = new TestResult { Name = "Telemetry", AmountFail = 0, AmountSuccess = 0 };
            IntegrationTestResult.AddFirst(OWCommandTest);
            IntegrationTestResult.AddFirst(TWCommandTest);
            IntegrationTestResult.AddFirst(EventTest);

            IntegrationTestResult.AddFirst(TelemetryTest);
            //
            var i = 0;

            // Test Loop
            while (!cts.Token.IsCancellationRequested && i < retries)
            {
                i++;
                // One Way Command Test
                try
                {
                    var commandName = "owcommand";
                    var commandPayload = @"mycommand payload";

                    // Send OW Command
                    backend.SendCommandOneWayAsync(deviceId, commandName, Encoding.UTF8.GetBytes(commandPayload)).Wait();

                    // Receive OW Command
                    var command = device.ReceiveCommandAsync().Result;
                    if (command.Payload != null)
                    {
                        var resultPayload = Encoding.UTF8.GetString(command.Payload);
                        var resultCommand = command.CommandName;
                        loggerDevice.LogInformation(String.Format("Command '{0}' Payload '{1}'", resultCommand, resultPayload));

                        if (commandName == resultCommand && resultPayload == commandPayload)
                        {
                            OWCommandTest.AmountSuccess++;
                        }
                        else
                        {
                            OWCommandTest.AmountFail++;
                        }
                    }
                    else
                    {
                        OWCommandTest.AmountFail++;
                    }
                }
                catch (Exception e)
                {
                    loggerDevice.LogError(e.StackTrace);
                    OWCommandTest.AmountFail++;
                }

                // Two Way Command Test - Malformed message error
                try
                {
                    var commandName = "twcommand";
                    var commandPayload = @"mycommand2 payload";
                    var fail = false;

                    backend.SendCommandTwoWayAsync(deviceId, commandName, Encoding.UTF8.GetBytes(commandPayload)).Wait();

                    var command = device.ReceiveCommandAsync().Result;

                    if (command.Payload != null)
                    {
                        var resultPayload = Encoding.UTF8.GetString(command.Payload);
                        var resultCommand = command.CommandName;
                        loggerDevice.LogInformation(String.Format("Received Command '{0}' with Payload '{1}'", resultCommand, resultPayload));

                        if (!(commandName == resultCommand && resultPayload == commandPayload))
                        {
                            fail = true;
                        }
                    }

                    string responsePayload = Guid.NewGuid().ToString();
                    if (command.RequestResponse)
                    {
                        loggerDevice.LogInformation(String.Format("Send command response with payload '{0}'", responsePayload));
                        device.SendCommandResponseAsync(command, 200, Encoding.UTF8.GetBytes(responsePayload)).Wait();
                    }

                    var response = backend.ReceiveCommandResponseAsync().Result;
                    Data data = (Data)response.BodySection;
                    if(!(response.ApplicationProperties["device_id"].ToString() == deviceId 
                        && response.ApplicationProperties["status"].ToString() == "200" 
                        && Encoding.UTF8.GetString(data.Binary) == responsePayload))
                    {
                        fail = true;
                    }


                    if (!fail)
                    {
                        TWCommandTest.AmountSuccess++;
                    }
                    else
                    {
                        TWCommandTest.AmountFail++;
                    }
                }
                catch (Exception e)
                {
                    loggerDevice.LogError(e.StackTrace);
                    TWCommandTest.AmountFail++;
                }


                // Event Test
                try
                {
                    var fail = false;
                    var eventPayload = @"myevent payload";

                    device.SendEventAsync(Encoding.UTF8.GetBytes(eventPayload)).Wait();
                    Message eventmsg;

                    do
                    {
                        eventmsg = backend.ReceiveEventAsync().Result;
                    } 
                    while (eventmsg.Properties.ContentType.ToString() == "application/vnd.eclipse-hono-empty-notification");

                    Data data = (Data)eventmsg.BodySection;
                    if (!(eventmsg.ApplicationProperties["device_id"].ToString() == deviceId
                        && Encoding.UTF8.GetString(data.Binary) == eventPayload))
                    {
                        fail = true;
                    }

                    if (!fail)
                    {
                        EventTest.AmountSuccess++;
                    }
                    else
                    {
                        EventTest.AmountFail++;
                    }
                }
                catch (Exception e)
                {
                    loggerDevice.LogError(e.StackTrace);
                    EventTest.AmountFail++;
                }
  
                // Telemetry Test
                try
                {
                    var fail = false;
                    var telemetryPayload = @"mytelemetry payload";

                    device.SendTelemetryAsync(Encoding.UTF8.GetBytes(telemetryPayload)).Wait();
                    var telmsg = backend.ReceiveTelemetryAsync().Result;

                    Data data = (Data)telmsg.BodySection;
                    if (!(telmsg.ApplicationProperties["device_id"].ToString() == deviceId
                        && Encoding.UTF8.GetString(data.Binary) == telemetryPayload))
                    {
                        fail = true;
                    }

                    if (!fail)
                    {
                        TelemetryTest.AmountSuccess++;
                    }
                    else
                    {
                        TelemetryTest.AmountFail++;
                    }
                }
                catch (Exception e)
                {
                    loggerDevice.LogError(e.StackTrace);
                    TelemetryTest.AmountFail++;
                }

                Task.Delay(500).Wait();
            }

            // Result Summary
            var success = true;
            foreach (var testresult in IntegrationTestResult)
            {
                success = success && testresult.Success();
                loggerTest.LogInformation($"Test {testresult.Name}: {testresult.SuccessString()} - Runs: {testresult.AmountSuccess+testresult.AmountFail} - Failures: {testresult.AmountFail}");
            }
            // Write result into file
            try
            {
                // If techcoil.txt exists, seek to the end of the file,
                // else create a new one.
                FileStream fileStream = File.Open("result.txt", FileMode.Create, FileAccess.Write);
                StreamWriter fileWriter = new StreamWriter(fileStream);
                if (success)
                {
                    fileWriter.WriteLine("success");
                }
                else
                {
                    fileWriter.WriteLine("failed");
                }
                fileWriter.Flush();
                fileWriter.Close();
            }
            catch (IOException ioe)
            {
                Console.WriteLine(ioe);
            }

        }

    }
}