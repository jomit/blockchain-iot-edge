#### Create & Build IoT Edge Solution

- Use VSCode to create an IoT edge solution
    - TODO parameters...

- Update the `deployment.template.json` file

- Login to ACR using VSCode or use `docker login edgeregistry.azurecr.io -u edgeregistry -p <password>`

- Right click on `deployment.template.json` file and click `Build IoT Edge Solution`

    - It should generate a new file `config\deployment.json` under the solution

#### Deploy Edge Solution

- Right click on the edge device in the Azure IOT Hub Devices explorer section and click `Create deployment for edge device`

    - Select the `config\deployment.json` under the solution

#### Configure Certificate

- https://docs.microsoft.com/en-us/azure/iot-edge/how-to-create-transparent-gateway

- Windows

    - Open Powershell in Administrator mode

    - `git clone -b modules-preview https://github.com/Azure/azure-iot-sdk-c.git`

    - `cd <edgefolder>`

    - `.  C:\repos\azure-iot-sdk-c\tools\CACertificates\ca-certs.ps1`

    - `Test-CACertsPrerequisites`

    - `New-CACertsCertChain rsa`

    - `New-CACertsEdgeDevice myedgegateway`

- Linux

    - Step 1 & Step 2 from https://github.com/Azure/azure-iot-sdk-c/blob/modules-preview/tools/CACertificates/CACertificateOverview.md

    - Make sure to upload the root certificate to Azrue IoT Hub

    - `./certGen.sh create_edge_device_certificate myGateway`

    - `cd certs`

    - `cat ./new-edge-device.cert.pem ./azure-iot-test-only.intermediate.cert.pem ./azure-iot-test-only.root.ca.cert.pem > ./new-edge-device-full-chain.cert.pem`

#### Setup Ubuntu VM for IoT Edge

- `sudo apt-get install python-pip`

- [Install Docker](https://docs.docker.com/install/linux/docker-ce/ubuntu/#set-up-the-repository)

    - `sudo apt-get update`

    - `sudo apt-get install apt-transport-https ca-certificates curl software-properties-common`

    - `curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -`

    - `sudo apt-key fingerprint 0EBFCD88`

    - `sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs)   stable"`

    - `sudo apt-get update`

    - `sudo apt-get install docker-ce`

    - `sudo docker run hello-world`

    - `sudo usermod -aG docker jomit`

- `sudo pip install -U azure-iot-edge-runtime-ctl`

#### Setup IoT Edge Runtime on a Windos Machine

- Edit hosts file "127.0.0.1 myedgegateway.test.com"

- Start docker on windows

- `pip install -U azure-iot-edge-runtime-ctl`
    - https://docs.microsoft.com/en-us/azure/iot-edge/quickstart

- `iotedgectl setup --connection-string "HostName=blockchain-hub.azure-devices.net;DeviceId=myedgedevice;SharedAccessKey=<key>" --edge-hostname "myedgegateway.test.com" --auto-cert-gen-force-no-passwords`

- `iotedgectl setup --connection-string "HostName=blockchain-hub.azure-devices.net;DeviceId=edgeAuditGateway;SharedAccessKey=<key>" --edge-hostname "myedgegateway.test.com" --device-ca-cert-file myedgegateway-public.pem --device-ca-chain-cert-file myedgegateway-all.pem --device-ca-private-key-file myedgegateway-private.pem --owner-ca-cert-file RootCA.pem`

- `iotedgectl login --address edgeregistry.azurecr.io --username edgeregistry --password <password>`

- `iotedgectl start`

- `docker ps`

- `docker logs -f edgeAgent`


#### OTHER

- Test default route for IoT Edge

    - Click on Iot Edge device in portal

    - Clickc on Set Modules -> Next -> [Make sure default route] -> Submit

    - `docker ps`

    - You should see a new `edgeHub` container

    - `docker logs -f edgeHub`

- Test device connection

    - Create new device on IoT Hub 

    - Update connection string in `device\app.js`

    - `npm start`

    - Start monitoring D2C message

- Create IoT Edge Module

    - `dotnet new -i Microsoft.Azure.IoT.Edge.Module`

    - `dotnet new aziotedgemodule -n TrackerModule`

    - Update the code in `Program.cs` inside `PipeMessage` function

    - Update the `repository` in `module.json` file to point to ACR

    - `docker login edgeregistry.azurecr.io -u edgeregistry -p <password>`

    - Right click on `module.json` and click `Build and Push IoT Edge Module Image`

- Deploy IoT Edge Module

    - In Azure Portal open the edge device and click on `Set Modules`

    - Click on `Add IoT Edge Module`
        - Name : `trackermodule`
        - Image URI :

    - Click Next and specify the route :

        ```{
            "routes": {
                "sensorToTrackerModule": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO BrokeredEndpoint(\"/modules/trackermodule/inputs/input1\")",
                "trackerModuleToIoTHub": "FROM /messages/modules/trackermodule/outputs/output1 INTO $upstream"
            }
        }```

    - `iotedgectl login --address edgeregistry.azurecr.io --username edgeregistry --password <password>`


#### Troubleshooting

- To resolve port mapping issues Reset Docker to factory defaults.

#### Additional Resources

- https://docs.microsoft.com/en-us/azure/iot-edge/how-to-create-transparent-gateway

- https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-csharp-module

- https://docs.microsoft.com/en-us/azure/iot-edge/how-to-vscode-develop-csharp-module

- https://github.com/Nethereum/Nethereum/blob/master/src/Nethereum.Tutorials/Nethereum.Tutorials.Core/CallTransactionEvents/CallTranEvents.cs

- https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted

- https://github.com/Nethereum/Nethereum/blob/master/src/Nethereum.Tutorials/Nethereum.Tutorials.Core/FunctionDTOs/FunctionDTO.cs



