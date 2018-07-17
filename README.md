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

- Create Azure Container Registry named `edgeregistry` with Admin User

#### Install Azure IoT Edge on Linux X64

- Create a new Ubuntu 16.04 VM and configure IoT Edge device as transparent gateway ([documentation](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-create-transparent-gateway-linux))
    - See `notes.txt` for configuration notes around certificate & edge config values


#### Install Azure IoT Edge on Linux ARM32 (Raspberry PI)

- [Deploy Azure IoT Edge runtime on Raspberry Pi](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-linux-arm)

- Connect with Raspberry Pi
    - `npm install -g device-discovery-cli`
    - `devdisco list --eth` or `devdisco list --wifi`
    
- [Upgrade jessie to stretch](https://www.raspberrypi.org/blog/raspbian-stretch/)
    - `sudo apt-get autoremove`
    - `sudo apt-get clean`

- Generate certificates and make IoT Edge device act as a transparent gateway
    - `./certGen.sh create_edge_device_certificate "piedgecert"`
    - `sudo systemctl restart iotedge`
    - `sudo systemctl status iotedge`
    - `sudo journalctl -u iotedge`
    - `journalctl -u iotedge --no-pager --no-full`
    - [Resolving libssl1.0.2 issue](https://github.com/MicrosoftDocs/azure-docs/issues/11046) 
        - `sudo apt remove docker-ce`
        - `dpkg --status libssl1.0.2`
        - `sudo apt update`
        - `sudo apt upgrade`
        - `sudo cat /etc/apt/sources.list`
        - `cat /etc/os-release`
        - `uname -m`
    - Install nodejs
        - `wget https://nodejs.org/dist/v8.11.3/node-v8.11.3-linux-armv7l.tar.xz`
        - `tar -xvf node-v8.11.3-linux-armv7l.tar.xz`
        - `cd node-v8.11.3-linux-armv7l.tar.xz`
        - `sudo cp -R * /usr/local/`

#### Build SmartContract and generate ABI and Binary

- Start Ganache `ganache-cli`
- Open command prompt and navigate to `smartcontracts` folder
- `truffle compile`
- `truffle test .\test\iotedgecontract.js`  (Make sure the unit tests are passing)
- `solc --abi .\contracts\IoTEdgeContract.sol`   (Copy ABI)
- `solc --bin .\contracts\IoTEdgeContract.sol`   (Copy Binary)

#### Test Smartcontract module code

- Start Ganache `ganache-cli --secure -u 0 -h <raspberry pi IP address>`
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

#### Deploy Smartcontract module

- In the Azure IoT Hub Devices explorer in Visual Studio Code, right click the edge device, click on `Create deployment for edge device` and select the `BlockchainEdge\config\deployment.json` file.
    - We can also do this using Azure Portal using Set Modules option.

#### Testing Smartcontract module via simulated leaf device

- Copy `device` folder
- Update IoT hub leaf device connection string
- `node app.js`

#### Testing Smartcontract module via thunderboard sensor device

- Copy `device` folder
- Update IoT hub leaf device connection string
- Start bluetooth on thunderboard
- `sudo node sensor.js`
- Install node-thunderboard-react nodejs package
        - `sudo npm install --unsafe-perm`

#### Additional Resources

- [Develop C# IoT Edge module on simulated device](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-csharp-module)

- [Managing Certificates for IoT Edge](https://github.com/Azure/azure-iot-sdk-c/blob/master/tools/CACertificates/CACertificateOverview.md)

- [Troubleshooting Azure IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/troubleshoot)

- Remove IoT Edge cache
    - `sudo rm /var/lib/iotedge/cache -f -d -r`
    - `sudo rm /var/lib/iotedge/hsm -f -d -r`
    - `sudo systemctl restart iotedge`

- Docker debugging
    - `sudo docker exec -it <containerid> bash`
    - `sudo docker logs -f <containerid>`
    - `sudo docker rm $(sudo docker ps -a -q) -f`
    - `sudo docker rmi $(sudo docker images -a -q) -f`
