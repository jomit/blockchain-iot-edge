namespace SmartContractModule
{
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
    using System.Collections.Generic;     // for KeyValuePair<>
    using Microsoft.Azure.Devices.Shared; // for TwinCollection
    using Newtonsoft.Json;                // for JsonConvert
    using Nethereum.Hex.HexTypes;
    using Nethereum.RPC.Eth.DTOs;
    using Nethereum.ABI.FunctionEncoding.Attributes;
    using Nethereum.Web3.Accounts.Managed;

    class Program
    {
        class MessageBody {
            public string deviceId { get; set; }
            public double temperature { get; set; }
            public double humidity { get; set; }
        }
        // class MessageBody
        // {
        //     public Machine machine { get; set; }
        //     public Ambient ambient { get; set; }
        //     public string timeCreated { get; set; }
        // }
        // class Machine
        // {
        //     public double temperature { get; set; }
        //     public double pressure { get; set; }
        // }
        // class Ambient
        // {
        //     public double temperature { get; set; }
        //     public int humidity { get; set; }
        // }
        static int counter;
        static int temperatureThreshold { get; set; } = 25;

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
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            // Register callback to be called when a message is received by the module
            // await ioTHubModuleClient.SetImputMessageHandlerAsync("input1", PipeMessage, iotHubModuleClient);

            // Read TemperatureThreshold from Module Twin Desired Properties
            var moduleTwin = await ioTHubModuleClient.GetTwinAsync();
            var moduleTwinCollection = moduleTwin.Properties.Desired;
            try
            {
                temperatureThreshold = moduleTwinCollection["TemperatureThreshold"];
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.WriteLine($"Property TemperatureThreshold not exist: {e.Message}");
            }

            // Attach callback for Twin desired properties updates
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(onDesiredPropertiesUpdate, null);

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", FilterMessages, ioTHubModuleClient);
        }

        static Task onDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Console.WriteLine("Desired property change:");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

                if (desiredProperties["TemperatureThreshold"] != null)
                    temperatureThreshold = desiredProperties["TemperatureThreshold"];

            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving desired property: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
            }
            return Task.CompletedTask;
        }

        static async Task<MessageResponse> FilterMessages(Message message, object userContext)
        {
            var counterValue = Interlocked.Increment(ref counter);
            try
            {
                ModuleClient moduleClient = (ModuleClient)userContext;
                var messageBytes = message.GetBytes();
                var messageString = Encoding.UTF8.GetString(messageBytes);
                Console.WriteLine($"Received message {counterValue}: [{messageString}]");

                // Get message body
                var messageBody = JsonConvert.DeserializeObject<MessageBody>(messageString);

                if (messageBody != null && messageBody.temperature < temperatureThreshold)
                {
                    Console.WriteLine($"Device temperature {messageBody.temperature} " +
                        $"is below threshold {temperatureThreshold}");
                    var filteredMessage = new Message(messageBytes);
                    foreach (KeyValuePair<string, string> prop in message.Properties)
                    {
                        filteredMessage.Properties.Add(prop.Key, prop.Value);
                    }

                    filteredMessage.Properties.Add("MessageType", "TempAlert");
                    RecordTransaction(message.ConnectionDeviceId, messageString).Wait();
                    await moduleClient.SendEventAsync("output1", filteredMessage);
                }

                // Indicate that the message treatment is completed
                return MessageResponse.Completed;
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error in sample: {0}", exception);
                }
                // Indicate that the message treatment is not completed
                var moduleClient = (ModuleClient)userContext;
                return MessageResponse.Abandoned;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
                // Indicate that the message treatment is not completed
                ModuleClient moduleClient = (ModuleClient)userContext;
                return MessageResponse.Abandoned;
            }
        }

        static async Task RecordTransaction(string deviceId, string recordData)
        {
            Console.WriteLine("Submitting transaction to Blockchain...");
            var rpcEndpoint = Environment.GetEnvironmentVariable("RPCENDPOINT");
            //var rpcEndpoint = "HTTP://127.0.0.1:8545";
            var abi = @"[{'constant':true,'inputs':[],'name':'getState','outputs':[{'name':'state','type':'uint8'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'deviceIdentifier','type':'string'},{'name':'data','type':'string'}],'name':'record','outputs':[{'name':'success','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'data','type':'string'}],'name':'settle','outputs':[{'name':'success','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'data','type':'string'}],'name':'verify','outputs':[{'name':'success','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'}]";
            var binary = "0x" + "608060405234801561001057600080fd5b506104ff806100206000396000f300608060405260043610610062576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff1680631865c57d14610067578063470bb62b14610098578063baf312eb1461015f578063bb9c6c3e146101e0575b600080fd5b34801561007357600080fd5b5061007c610261565b604051808260ff1660ff16815260200191505060405180910390f35b3480156100a457600080fd5b50610145600480360381019080803590602001908201803590602001908080601f0160208091040260200160405190810160405280939291908181526020018383808284378201915050505050509192919290803590602001908201803590602001908080601f0160208091040260200160405190810160405280939291908181526020018383808284378201915050505050509192919290505050610283565b604051808215151515815260200191505060405180910390f35b34801561016b57600080fd5b506101c6600480360381019080803590602001908201803590602001908080601f0160208091040260200160405190810160405280939291908181526020018383808284378201915050505050509192919290505050610321565b604051808215151515815260200191505060405180910390f35b3480156101ec57600080fd5b50610247600480360381019080803590602001908201803590602001908080601f01602080910402602001604051908101604052809392919081815260200183838082843782019150505050505091929192905050506103a8565b604051808215151515815260200191505060405180910390f35b6000600160009054906101000a900460ff16600281111561027e57fe5b905090565b6000826000908051906020019061029b92919061042e565b5081600290805190602001906102b292919061042e565b50336001806101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506000600160006101000a81548160ff0219169083600281111561031257fe5b02179055506001905092915050565b6000816006908051906020019061033992919061042e565b5033600560006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506002600160006101000a81548160ff0219169083600281111561039a57fe5b021790555060019050919050565b600081600490805190602001906103c092919061042e565b5033600360006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff16021790555060018060006101000a81548160ff0219169083600281111561042057fe5b021790555060019050919050565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061046f57805160ff191683800117855561049d565b8280016001018555821561049d579182015b8281111561049c578251825591602001919060010190610481565b5b5090506104aa91906104ae565b5090565b6104d091905b808211156104cc5760008160009055506001016104b4565b5090565b905600a165627a7a72305820b5197f569531ac9920f3c33b09dfd1aaab4a0348a8216d2939882ed14527a4350029";

            Console.WriteLine($"Connecting to {rpcEndpoint}");
            var tempWeb3 = new Nethereum.Geth.Web3Geth(rpcEndpoint);
            var senderAddress = await tempWeb3.Eth.CoinBase.SendRequestAsync();

            Console.WriteLine($"Sender Address => {senderAddress}");
            var account = new ManagedAccount(senderAddress, "qwqw");
            var web3 = new Nethereum.Geth.Web3Geth(account, rpcEndpoint);

            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(binary, senderAddress, new HexBigInteger(900000));
            Console.WriteLine($"Deploy Contract Transaction Hash  => {transactionHash}");
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            var contractAddress = receipt.ContractAddress;
            Console.WriteLine($"Contract Address => {contractAddress}");
            var contract = web3.Eth.GetContract(abi, contractAddress);
            var recordFunction = contract.GetFunction("record");
            var getStateFunction = contract.GetFunction("getState");

            transactionHash = await recordFunction.SendTransactionAsync(
                senderAddress, new HexBigInteger(900000), null, deviceId, recordData);
            Console.WriteLine($"[Record] Transaction Hash  => {transactionHash}");
            receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

            var result = await getStateFunction.CallAsync<int>();
            Console.WriteLine($"Added Blockchain transaction => {result}");
        }
    }
}
