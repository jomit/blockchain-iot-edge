'use strict';

var fs = require('fs');
var clientFromConnectionString = require('azure-iot-device-mqtt').clientFromConnectionString;
var Message = require('azure-iot-device').Message;

var deviceId = 'pileafsensor';
var deviceConnectionString = "HostName=blockchain-hub.azure-devices.net;DeviceId=pileafsensor;SharedAccessKey=<key>";
var edgeHostName = "raspberrypi";
var connectionString = deviceConnectionString + ";GatewayHostName=" + edgeHostName;
console.log(connectionString);
var client = clientFromConnectionString(connectionString);

var ThunderboardReact = require('node-thunderboard-react');
var thunder = new ThunderboardReact();

var connectCallBack = function (err) {
    if (err) {
        console.error("Could not connect: " + err.message);
    } else {
        console.log("Client connected");
        // client.on("message", function (msg) {
        //     console.log("Id: " + msg.messageId + "Body: " + msg.data);
        //     client.complete(msg, sendMessageCallback);
        // });

        connectDeviceAndStartMonitoring();

        // client.on('error', function (err) {
        //     console.error(err.message);
        // });

        // client.on("disconnect", function () {
        //     client.removeAllListeners();
        //     client.open(connectCallBack);
        // });
    }
};

function connectDeviceAndStartMonitoring() {
    thunder.init((error) => {
        thunder.startDiscovery((device) => {
            console.log('- Found ' + device.localName);
            thunder.stopDiscovery();
            device.connect((error) => {
                console.log('- Connected ' + device.localName);
                sendSensorData(device);
            });
        });
    });
}

function sendSensorData(device) {
    device.getAmbientLight((error, res) => {
        var temperature = res.lux; //sending ambient light instead of temperature as its easier for testing
        var data = JSON.stringify({ deviceId: deviceId, temperature: temperature });
        var message = new Message(data);
        console.log("Ambient Light message : " + message.getData());
        client.sendEvent(message, sendMessageCallback);
        setTimeout(sendSensorData.bind(null,device), 1000);
    });
}

function sendMessageCallback(err) {
    if (err) {
        console.log('Send Message error: ' + err.toString());
    } else {
        console.log('Message Sent..');
    }
}

var options  = {
    ca : fs.readFileSync("/home/pi/gatewaycerts/certs/azure-iot-test-only.root.ca.cert.pem", "utf-8").toString()
};

client.setOptions(options);
client.open(connectCallBack);