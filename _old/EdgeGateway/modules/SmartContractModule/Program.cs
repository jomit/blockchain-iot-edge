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
    using Nethereum.ABI.FunctionEncoding.Attributes;
    using Nethereum.Web3.Accounts.Managed;
    using Nethereum.Hex.HexTypes;

    class Program
    {
        static int counter;

        static void Main(string[] args)
        {
            // The Edge runtime gives us the connection string we need -- it is injected as an environment variable
            string connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

            // Cert verification is not yet fully functional when using Windows OS for the container
            //[JV = > Need to hardcode this to true due to a cert validation bug in Windows ???]
            bool bypassCertVerification = true; //RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (!bypassCertVerification) InstallCert();
            Init(connectionString, bypassCertVerification).Wait();

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
        /// Add certificate in local cert store for use by client for secure connection to IoT Edge runtime
        /// </summary>
        static void InstallCert()
        {
            string certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
            if (string.IsNullOrWhiteSpace(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing path to certificate file.");
            }
            else if (!File.Exists(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing certificate file.");
            }
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile(certPath)));
            Console.WriteLine("Added Cert: " + certPath);
            store.Close();
        }


        /// <summary>
        /// Initializes the DeviceClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init(string connectionString, bool bypassCertVerification = false)
        {
            Console.WriteLine("Connection String {0}", connectionString);

            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            // During dev you might want to bypass the cert verification. It is highly recommended to verify certs systematically in production
            if (bypassCertVerification)
            {
                mqttSetting.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            DeviceClient ioTHubModuleClient = DeviceClient.CreateFromConnectionString(connectionString, settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", PipeMessage, ioTHubModuleClient);
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> PipeMessage(Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref counter);

            var deviceClient = userContext as DeviceClient;
            if (deviceClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");

            //[TODO] : Add logic to only call this in case of a contrat violation
            RecordTransaction(message.ConnectionDeviceId, messageString).Wait();

            if (!string.IsNullOrEmpty(messageString))
            {
                var pipeMessage = new Message(messageBytes);
                foreach (var prop in message.Properties)
                {
                    pipeMessage.Properties.Add(prop.Key, prop.Value);
                }
                await deviceClient.SendEventAsync("output1", pipeMessage);
                Console.WriteLine("Received message sent");
            }
            return MessageResponse.Completed;
        }

        static async Task RecordTransaction(string deviceId, string recordData)
        {
            Console.WriteLine("Submitting transaction to Blockchain...");
            var rpcEndpoint = Environment.GetEnvironmentVariable("RPCENDPOINT");
            var abi = @"[{'constant':false,'inputs':[],'name':'getState','outputs':[{'name':'state','type':'uint8'}],'payable':false,'type':'function'},{'constant':false,'inputs':[{'name':'deviceIdentifier','type':'string'},{'name':'data','type':'string'}],'name':'record','outputs':[{'name':'success','type':'bool'}],'payable':false,'type':'function'},{'constant':false,'inputs':[{'name':'data','type':'string'}],'name':'settle','outputs':[{'name':'success','type':'bool'}],'payable':false,'type':'function'},{'constant':false,'inputs':[{'name':'data','type':'string'}],'name':'verify','outputs':[{'name':'success','type':'bool'}],'payable':false,'type':'function'}]";
            var byteCode = "0x6060604052341561000f57600080fd5b5b6104d28061001f6000396000f30060606040526000357c0100000000000000000000000000000000000000000000000000000000900463ffffffff1680631865c57d1461005f578063470bb62b1461008e578063baf312eb14610146578063bb9c6c3e146101bb575b600080fd5b341561006a57600080fd5b610072610230565b604051808260ff1660ff16815260200191505060405180910390f35b341561009957600080fd5b61012c600480803590602001908201803590602001908080601f0160208091040260200160405190810160405280939291908181526020018383808284378201915050505050509190803590602001908201803590602001908080601f01602080910402602001604051908101604052809392919081815260200183838082843782019150505050505091905050610253565b604051808215151515815260200191505060405180910390f35b341561015157600080fd5b6101a1600480803590602001908201803590602001908080601f016020809104026020016040519081016040528093929190818152602001838380828437820191505050505050919050506102f2565b604051808215151515815260200191505060405180910390f35b34156101c657600080fd5b610216600480803590602001908201803590602001908080601f0160208091040260200160405190810160405280939291908181526020018383808284378201915050505050509190505061037a565b604051808215151515815260200191505060405180910390f35b6000600160009054906101000a900460ff16600281111561024d57fe5b90505b90565b6000826000908051906020019061026b929190610401565b508160029080519060200190610282929190610401565b50336001806101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506000600160006101000a81548160ff021916908360028111156102e257fe5b0217905550600190505b92915050565b6000816006908051906020019061030a929190610401565b5033600560006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506002600160006101000a81548160ff0219169083600281111561036b57fe5b0217905550600190505b919050565b60008160049080519060200190610392929190610401565b5033600360006101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff16021790555060018060006101000a81548160ff021916908360028111156103f257fe5b0217905550600190505b919050565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f1061044257805160ff1916838001178555610470565b82800160010185558215610470579182015b8281111561046f578251825591602001919060010190610454565b5b50905061047d9190610481565b5090565b6104a391905b8082111561049f576000816000905550600101610487565b5090565b905600a165627a7a72305820ab727d347585703d5476ab97e2fc08699135b4102f2866ffca72290d4f573f2c0029";

            Console.WriteLine($"Connecting to {rpcEndpoint}");
            var tempWeb3 = new Nethereum.Geth.Web3Geth(rpcEndpoint);
            var senderAddress = await tempWeb3.Eth.CoinBase.SendRequestAsync();

            Console.WriteLine($"Sender Address => {senderAddress}");
            var account = new ManagedAccount(senderAddress, "");
            var web3 = new Nethereum.Geth.Web3Geth(account, rpcEndpoint);

            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(byteCode, senderAddress, new HexBigInteger(900000));
            //Console.WriteLine($"Deploy Contract Transaction Hash  => {transactionHash}");
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            var contractAddress = receipt.ContractAddress;
            Console.WriteLine($"Contract Address => {contractAddress}");
            var contract = web3.Eth.GetContract(abi, contractAddress);
            var recordFunction = contract.GetFunction("record");
            var getStateFunction = contract.GetFunction("getState");

            transactionHash = await recordFunction.SendTransactionAsync(senderAddress, new HexBigInteger(900000), null, deviceId, recordData);
            Console.WriteLine($"[Record] Transaction Hash  => {transactionHash}");
            //receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

            //var result = await getStateFunction.CallAsync<int>();
            //Console.WriteLine($"Added Blockchain transaction => {result}");
        }
    }
}
