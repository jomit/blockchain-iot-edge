# Azure IoT Edge Smart Contract Module for Blockchain 

![Azure IoT Edge Smart Contract Module for Blockchain](https://raw.githubusercontent.com/jomit/blockchain-iot-edge/master/smart-contract-module.jpg)

#### Prerequisites

- Install [Visual Studio Code](https://code.visualstudio.com/)

- Install [Azure IoT Toolkit for VSCode](https://marketplace.visualstudio.com/items?itemName=vsciot-vscode.azure-iot-toolkit) extension

- Install [Azure IoT Edge for VSCode](https://marketplace.visualstudio.com/items?itemName=vsciot-vscode.azure-iot-edge) extension

- Install [Docker for Windows](https://docs.docker.com/docker-for-windows/install/)

- Install [.NET Core SDK](https://www.microsoft.com/net/core#windowscmd)

- Install [Truffle](https://truffleframework.com/)

- Install [ganache-cli](https://github.com/trufflesuite/ganache-cli)

- Install [Solidity Compiler](https://github.com/ethereum/solidity/releases/tag/v0.4.24) and set it on your PATH

#### Setup

- Create IoT Hub, IoT Edge Device and Leaf Device in Azure Portal ([documentation](https://docs.microsoft.com/en-us/azure/iot-edge/quickstart-linux))

- Create a new Ubuntu 16.04 VM and configure IoT Edge device as transparent gateway ([documentation](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-create-transparent-gateway-linux))
    - See `notes.txt` for configuration notes around certificate & edge config values

- Create Azure Container Registry named `edgeregistry` with Admin User

#### Build SmartContract and generate ABI and Binary

- Start Ganache `ganache-cli`

- Open command prompt and navigate to `smartcontracts` folder

- `truffle compile`

- `truffle test .\test\iotedgecontract.js`  (Make sure the unit tests are passing)

- `solc --abi .\contracts\IoTEdgeContract.sol`   (Copy ABI)

- `solc --bin .\contracts\IoTEdgeContract.sol`   (Copy Binary)

#### Test Smartcontract module code

- Start Ganache `ganache-cli --secure -u 0`
    - The -u option is to unlock the account, see ganache-cli options for more details.

- In `testing\Program.cs` update 
    - `abi` and `binary` variables with the ones copied above
    - `rpcEndpoint` variable with the RPC Server value shown in Ganache

- `cd testing`

- `dotnet restore`

- `dotnet run`

#### Build SmartContract IoT Edge Module 

- Update `RecordTransaction` method in `BlockchainEdge\modules\SmartContractModule\Program.cs`

- Start Docker for Windows on your laptop

- Login to Azure Container Registry
    - `docker login edgeregistry.azurecr.io -u edgeregistry -p <password>`

- In Visual Studio Code, right click on `BlockchainEdge\deployment.template.json` file and click `Build IoT Edge Solution`. This will build the container images and push to ACR.

- InProgress...

#### Deploy Smartcontract module

- InProgress...

#### Testing Smartcontract module via leaf device

- InProgress...

#### Additional Resources

- [Develop C# IoT Edge module on simulated device](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-csharp-module)

- [Managing Certificates for IoT Edge](https://github.com/Azure/azure-iot-sdk-c/blob/master/tools/CACertificates/CACertificateOverview.md)

- [Troubleshooting Azure IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/troubleshoot)

- Remove IoT Edge cache
    - `sudo rm /var/lib/iotedge/cache -f -d -r`
    - `sudo rm /var/lib/iotedge/hsm -f -d -r`
    - `sudo systemctl restart iotedge`
