var IoTEdgeContract = artifacts.require("./IoTEdgeContract.sol");

module.exports = function(deployer) {
  deployer.deploy(IoTEdgeContract, "device01","{ temperature : 100 }");
};
