pragma solidity ^0.4.10;

contract IoTEdgeContract {

    enum States {Recorded, Verified, Settled}

    string deviceId;
    States currentState;

    address recorder;
    string recordData;
    
    address verifier;
    string verifiedData;
    
    address settler;
    string setteledData;
    
    function record(string deviceIdentifier, string data) public returns (bool success) {
        deviceId = deviceIdentifier;
        recordData = data;
        recorder = msg.sender;
        currentState = States.Recorded;
        return true;
    }

    function verify(string data) public returns (bool success) {
        verifiedData = data;
        verifier = msg.sender;
        currentState = States.Verified;
        return true;
    }

    function settle(string data) public returns (bool success) {
        setteledData = data;
        settler = msg.sender;
        currentState = States.Settled;
        return true;
    }

    function getState() public returns (uint8 state) {
        return uint8(currentState);
    }
}