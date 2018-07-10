var IoTEdgeContract = artifacts.require("./IoTEdgeContract.sol");

module.exports = function(deployer) {
  deployer.deploy(IoTEdgeContract);
};
