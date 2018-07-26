var IoTEdgeContract = artifacts.require("./IoTEdgeContract.sol");

contract('IoTEdgeContract', function (accounts) {
  it("happy path", function () {
    var iotedgecontract;
    return IoTEdgeContract.deployed().then(function (instance) {
      iotedgecontract = instance;
      return iotedgecontract.getState.call();
      //return iotedgecontract.record("myleafdevice", "{ 'temperature' : 400, 'humidity' : 50 }", { from: accounts[1] });
    }).then(function (result) {
      //console.log(result);
      assert.equal(0,result);
    });
  });
});