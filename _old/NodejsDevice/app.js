'use strict';

var clientFromConnectionString = require('azure-iot-device-mqtt').clientFromConnectionString;
var Message = require('azure-iot-device').Message;

var deviceId = 'leafsensor';
var deviceConnectionString = "HostName=blockchain-hub.azure-devices.net;DeviceId=leafsensor;SharedAccessKey=<key>";
var edgeHostName = "myedgegateway.test.com" 
var connectionString = deviceConnectionString + ";GatewayHostName=" + edgeHostName;
console.log(connectionString);
var client = clientFromConnectionString(connectionString);

function printResultFor(op) {
    return function printResult(err, res) {
        if (err) console.log(op + ' error: ' + err.toString());
        if (res) console.log(op + ' status: ' + res.constructor.name);
    };
}

var connectCallback = function (err) {
    if (err) {
        console.log('Connection Error: ' + err);
    } else {
        console.log('Client connected');

        // Create a message and send it to the IoT Hub every second
        setInterval(function () {
            var temperature = 20 + (Math.random() * 15);
            var humidity = 60 + (Math.random() * 20);
            var data = JSON.stringify({ deviceId: deviceId, temperature: temperature, humidity: humidity });
            var message = new Message(data);
            //message.properties.add('temperatureAlert', (temperature > 30) ? 'true' : 'false');
            console.log("Sending message: " + message.getData());
            client.sendEvent(message, printResultFor('send'));
        }, 2000);
    }
};

client.open(connectCallback);