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
        class MessageBody
        {
            public string deviceId { get; set; }
            public double temperature { get; set; }
            public double humidity { get; set; }
        }
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

            //Testing
            //var messageString = "{ 'temp' : 100, 'humidity': 50 }";
            //RecordTransaction("testdevice", messageString).Wait();
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
                    await RecordTransaction(message.ConnectionDeviceId, messageString);
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
            try
            {
                Console.WriteLine("Submitting transaction to Blockchain...");
                var rpcEndpoint = Environment.GetEnvironmentVariable("RPCENDPOINT");
                var abi = @"[{'constant':true,'inputs':[],'name':'getState','outputs':[{'name':'state','type':'uint8'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'data','type':'string'}],'name':'settle','outputs':[{'name':'success','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'data','type':'string'}],'name':'verify','outputs':[{'name':'success','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[{'name':'deviceIdentifier','type':'string'},{'name':'data','type':'string'}],'payable':false,'stateMutability':'nonpayable','type':'constructor'}]";
                var binary = "0x" + "608060405234801561001057600080fd5b50604051610519380380610519833981018060405281019080805182019291906020018051820192919050505081600090805190602001906100539291906100d6565b50806002908051906020019061006a9291906100d6565b50336001806101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506000600160006101000a81548160ff021916908360028111156100ca57fe5b0217905550505061017b565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061011757805160ff1916838001178555610145565b82800160010185558215610145579182015b82811115610144578251825591602001919060010190610129565b5b5090506101529190610156565b5090565b61017891905b8082111561017457600081600090555060010161015c565b5090565b90565b61038f8061018a6000396000f300608060405260043610610057576000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff1680631865c57d1461005c578063baf312eb1461008d578063bb9c6c3e1461010e575b600080fd5b34801561006857600080fd5b5061007161018f565b604051808260ff1660ff16815260200191505060405180910390f35b34801561009957600080fd5b506100f4600480360381019080803590602001908201803590602001908080601f01602080910402602001604051908101604052809392919081815260200183838082843782019150505050505091929192905050506101b1565b604051808215151515815260200191505060405180910390f35b34801561011a57600080fd5b50610175600480360381019080803590602001908201803590602001908080601f0160208091040260200160405190810160405280939291908181526020018383808284378201915050505050509192919290505050610238565b604051808215151515815260200191505060405180910390f35b6000600160009054906101000a900460ff1660028111156101ac57fe5b905090565b600081600690805190602001906101c99291906102be565b5033600560006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506002600160006101000a81548160ff0219169083600281111561022a57fe5b021790555060019050919050565b600081600490805190602001906102509291906102be565b5033600360006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff16021790555060018060006101000a81548160ff021916908360028111156102b057fe5b021790555060019050919050565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f106102ff57805160ff191683800117855561032d565b8280016001018555821561032d579182015b8281111561032c578251825591602001919060010190610311565b5b50905061033a919061033e565b5090565b61036091905b8082111561035c576000816000905550600101610344565b5090565b905600a165627a7a72305820533027bbe7ff6dba6aa8c76061443be069b94d6762bfa54f53517dde2c276bad0029";

                //Console.WriteLine($"Connecting to rpcendpoint : {rpcEndpoint}");
                //var tempWeb3 = new Nethereum.Geth.Web3Geth(rpcEndpoint);
                //var senderAddress = await tempWeb3.Eth.CoinBase.SendRequestAsync();
                var senderAddress = Environment.GetEnvironmentVariable("SENDER_ADDRESS"); 
                var senderPassword = Environment.GetEnvironmentVariable("SENDER_PASSWORD"); 

                Console.WriteLine($"Sender Address => {senderAddress}");
                var account = new ManagedAccount(senderAddress, senderPassword);
                var web3 = new Nethereum.Geth.Web3Geth(account, rpcEndpoint);

                var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(abi,binary, senderAddress, new HexBigInteger(900000), null, null, deviceId, recordData);
                Console.WriteLine($"Added Blockchain contract transaction => {transactionHash}");
                /*var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
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
                Console.WriteLine($"Added Blockchain transaction => {result}");*/
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
