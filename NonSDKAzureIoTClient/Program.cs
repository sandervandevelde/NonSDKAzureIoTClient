using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;


namespace NonSDKAzureIoTClient
{
    internal class Program
    {
        private static Topics topics = null;

        private static IMqttClient mqttClient = null;

        private static MqttFactory mqttFactory = null;

        static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            Settings settings = config.GetRequiredSection("Settings").Get<Settings>();

            Console.WriteLine("Hello non-SDK MQTT Client for IoT Hub!");

            var user_name = $"{settings.hostName}/{settings.clientId}/?api-version=2021-04-12";
            var passw = $"SharedAccessSignature sr={settings.hostName}%2Fdevices%2F{settings.clientId}&sig={settings.sasSig}&se={settings.sasSe}"; // SAS key

            topics = new Topics(settings.clientId);

            try
            {
                mqttFactory = await ConnectMQTTClientToCloud(settings.hostName, settings.clientId, user_name, passw);

                await RegisterMQTTTopicsForIncomingMessages();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }

            Console.WriteLine("MQTT client subscribed to topic.");

            var thread = new Thread(() => ThreadBody());
            thread.Start();

            // Just keep the app running
            Console.ReadKey();
        }

        private static async Task RegisterMQTTTopicsForIncomingMessages()
        {
            //// subscribe for cloud messages. Note: "cleansession:0" is used to receive offline, already queued, messages
            var mqttSubscribeOptionsCloudMessage = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => { f.WithTopic(topics.subscribe_topic_filter_cloudmessage); })
                .Build();
            await mqttClient.SubscribeAsync(mqttSubscribeOptionsCloudMessage, CancellationToken.None);

            //// subscribe for direct methods
            var mqttSubscribeOptionsdirectmethod = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => { f.WithTopic(topics.subscribe_topic_filter_directmethod); })
                .Build();
            await mqttClient.SubscribeAsync(mqttSubscribeOptionsdirectmethod, CancellationToken.None);

            //// subscribe for desired properties
            var mqttSubscribeOptionsdesiredprop = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => { f.WithTopic(topics.subscribe_topic_filter_desiredprop); })
                .Build();
            await mqttClient.SubscribeAsync(mqttSubscribeOptionsdesiredprop, CancellationToken.None);

            //// subscribe for operation response; so we can receive both the device twin on start up and reported properties change confirmation
            var mqttSubscribeOptionsreportedpropresponse = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => { f.WithTopic(topics.subscribe_topic_filter_operationresponse); })
                .Build();
            await mqttClient.SubscribeAsync(mqttSubscribeOptionsreportedpropresponse, CancellationToken.None);
        }

        private static async Task<MqttFactory> ConnectMQTTClientToCloud(string hostName, string clientId, string user_name, string passw, bool cleanSession = false)
        {
            // connect
            var mqttFactory = new MqttFactory();

            mqttClient = mqttFactory.CreateMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(hostName, 8883)
                .WithCredentials(user_name, passw)
                .WithClientId(clientId)
                .WithCleanSession(cleanSession) // if false, receive queued cloud messages while offline 
                .WithTls(new MqttClientOptionsBuilderTlsParameters()
                {
                    AllowUntrustedCertificates = true,
                    Certificates = new List<X509Certificate>
                    {
                        //// BE AWARE, this certificate will expire in March 2023! See also the readme.md
                        new X509Certificate2("baltimore.cer")
                    },
                    UseTls = true,
                })
                .Build();

            // listen for messages received
            mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            return mqttFactory;
        }

        private static Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            //// Desired properties change received as event
            if (args.ApplicationMessage.Topic.Contains("$iothub/twin/PATCH/properties/desired/"))
            {
                var requestid = Convert.ToInt32(args.ApplicationMessage.Topic.Split('=')[1]);

                Console.WriteLine($"Desired properties event received with id {requestid} :");

                Console.WriteLine($"Received application message: {args.ApplicationMessage.ConvertPayloadToString()}");

                return Task.CompletedTask;
            }

            //// Cloud message received
            if (args.ApplicationMessage.Topic.Contains("devicebound"))
            {
                Console.WriteLine("Cloud message event received:");

                Console.WriteLine($"Received application message: {args.ApplicationMessage.ConvertPayloadToString()}");

                //// Notice the topic contains both 'devicebound' and 'deviceBound'

                if (args.ApplicationMessage.Topic.ToLower().Split("devicebound").Length > 2)
                {
                    var properties = args.ApplicationMessage.Topic.ToLower().Split("devicebound")[2];

                    var propertyList = properties.Split('&', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var item in propertyList)
                    {
                        var p = item.Split('=');
                        Console.WriteLine($"\tProperty: {p[0]}={p[1]}");
                    }
                }

                return Task.CompletedTask;
            }

            // direct method received
            if (args.ApplicationMessage.Topic.Contains("$iothub/methods/POST/"))
            {
                var methodName = args.ApplicationMessage.Topic.Split('/')[3];
                var requestid = Convert.ToInt32(args.ApplicationMessage.Topic.Split('=')[1]);

                Console.WriteLine($"Direct method {methodName} event received with id {requestid}:");

                Console.WriteLine($"Received application message: {args.ApplicationMessage.ConvertPayloadToString()}");

                //// Return the Direct Method response.

                var payloadJsonDirectMethod = new JObject();
                payloadJsonDirectMethod.Add("ans", 42);

                string payloadStringDirectMethod = JsonConvert.SerializeObject(payloadJsonDirectMethod);

                var messageDirectMethod = new MqttApplicationMessageBuilder()
                .WithTopic($"{topics.send_direct_method_response_topic}{requestid}") // 200 = success
                .WithPayload(payloadStringDirectMethod)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

                mqttClient.PublishAsync(messageDirectMethod).Wait();

                Console.WriteLine("Direct method response sent");

                return Task.CompletedTask;
            }

            // DeviceTwin received
            if (args.ApplicationMessage.Topic.Contains("$iothub/twin/res/200"))
            {
                var requestid = Convert.ToInt32(args.ApplicationMessage.Topic.Split('=')[1]);

                Console.WriteLine($"Device Twin response on request received with id {requestid}:");

                Console.WriteLine($"Received message: {args.ApplicationMessage.ConvertPayloadToString()}");

                return Task.CompletedTask;
            }

            // Reported properties response to update received
            if (args.ApplicationMessage.Topic.Contains("$iothub/twin/res/204"))
            {
                var parameters = args.ApplicationMessage.Topic.Split('=')[1];

                var requestid = Convert.ToInt32(parameters.Split('&')[0]); 

                var version = Convert.ToInt32(args.ApplicationMessage.Topic.Split('=')[2]);

                Console.WriteLine($"Reported Twin response on request received with id {requestid} and version {version}");

                Console.WriteLine($"Received message: '{args.ApplicationMessage.ConvertPayloadToString()??"Empty payload"}' as response");

                return Task.CompletedTask;
            }

            Console.WriteLine($"UNHANDLED '{args.ApplicationMessage.Topic}' RECEIVED");

            return Task.CompletedTask;
        }

        private static async void ThreadBody()
        {
            await RequestLatestDeviceTwinFromCloud();

            SendReportedPropertiesUpdateToCloud();

            while (true)
            {
                var payloadJson = new JObject();
                payloadJson.Add("temp", DateTime.UtcNow.Millisecond % 20);
                payloadJson.Add("hum", DateTime.UtcNow.Millisecond / 10);

                string payloadString = JsonConvert.SerializeObject(payloadJson);

                Console.Write($"Sending message:{payloadString}... ");

                //// User properties must be made part of the topic. The .WithUserProperty is ignored by the IoT Hub
                //// if iot hub routig based on body properties is needed, add $.ct=application%2Fjson&$.ce=utf-8 . Notice the encoding

                var message = new MqttApplicationMessageBuilder()
                   .WithTopic(topics.send_message_topic + "$.ct=application%2Fjson&$.ce=utf-8" + "&a=a1&b=bb2")
                   .WithPayload(payloadString)
                   .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

                await mqttClient.PublishAsync(message);
                Console.WriteLine("sent");

                Thread.Sleep(10000);
            }
        }

        private static async Task RequestLatestDeviceTwinFromCloud()
        {
            // request for latest devicetwin
            var messageDeviceTwin = new MqttApplicationMessageBuilder()
               .WithTopic(topics.request_latest_device_twin_topic)
               .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
            await mqttClient.PublishAsync(messageDeviceTwin);
            Console.WriteLine("Requested latest DeviceTwin with request id 42");
        }

        private static void SendReportedPropertiesUpdateToCloud()
        {
            // send reported property
            var payloadStringReportedProp = "{ \"telemetrySendFrequency\": \"" + DateTime.Now.Second.ToString() + "m\"}";
            var messageReportedProp = new MqttApplicationMessageBuilder()
                .WithTopic(topics.send_reported_properties_topic)
                .WithPayload(payloadStringReportedProp)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            mqttClient.PublishAsync(messageReportedProp).Wait();
            Console.WriteLine($"Reported properties update sent: '{payloadStringReportedProp}'");
        }
    }
}
