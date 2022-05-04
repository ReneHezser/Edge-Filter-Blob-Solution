using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Newtonsoft.Json.Linq;

namespace FilterModule
{
    class Program
    {
        static int counter;
        static string containerName = "edge";
        static BlobContainerClient containerClient;

        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            try
            {
                await ConnectToStorage();

                MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only);
                ITransportSettings[] settings = { mqttSetting };

                // Open a connection to the Edge runtime
                ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
                await ioTHubModuleClient.OpenAsync();
                Console.WriteLine("IoT Hub module client initialized.");

                // Register callback to be called when a message is received by the module
                await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", PipeMessage, ioTHubModuleClient);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error in Init: {ex.ToString()}");
                throw;
            }
        }

        /// <summary>
        /// Connect to the storage account container
        /// </summary>
        /// <returns></returns>
        static async Task ConnectToStorage()
        {
            var connectionString = Environment.GetEnvironmentVariable("StorageAccountConnectionString");
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("StorageAccountConnectionString");

            var blobServiceClient = new BlobServiceClient(connectionString);
            containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> PipeMessage(Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref counter);

            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");

            if (!string.IsNullOrEmpty(messageString))
            {
                using (var pipeMessage = new Message(messageBytes))
                {
                    foreach (var prop in message.Properties)
                    {
                        pipeMessage.Properties.Add(prop.Key, prop.Value);
                    }
                    await moduleClient.SendEventAsync("output1", pipeMessage);

                    Console.WriteLine("Received message sent");
                }

                // if the message is not empty, trigger upload logic
                await UploadBlob(messageString, messageBytes);

            }
            return MessageResponse.Completed;
        }

        /// <summary>
        /// Upload the message as blob
        /// </summary>
        /// <param name="messageString"></param>
        /// <param name="messageBytes"></param>
        /// <returns></returns>
        private static async Task UploadBlob(string messageString, byte[] messageBytes)
        {
            try
            {
                // "{\"machine\":{\"temperature\":25.645135668756033,\"pressure\":1.529192671124105},\"ambient\":{\"temperature\":21.05118248544223,\"humidity\":26},\"timeCreated\":\"2022-05-03T12:43:07.2025202Z\"}"
                JObject json = (JObject)JObject.Parse(messageString);
                if (json["machine"]?["temperature"]?.Value<float>() > 25.0)
                {
                    // use the timestamp as filename
                    var filename = ((DateTime)json["timeCreated"]?.Value<DateTime?>()).ToString("o", System.Globalization.CultureInfo.InvariantCulture).Replace(':', '_') + ".json";
                    Console.WriteLine($"Uploading blob '{filename}");
                    var binaryData = new BinaryData(messageBytes);
                    var blobClient = containerClient.GetBlobClient(filename);
                    // create a new blob without overwriting existing blobs
                    await blobClient.UploadAsync(binaryData, false);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading to Blob Storage: {ex.ToString()}");
            }
        }
    }
}
