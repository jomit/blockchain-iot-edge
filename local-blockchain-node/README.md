# Setup

- Create Ethereum Cluster on Azure

- Get Enode url and genesis file

    - SSH into one of the transaction nodes `publicip:4000`

    - `geth attach`

    - `admin.nodeInfo.enode`

    -`enode://33d11d36d22731363e1d6b7ecee024a7dde431f224e073e0b2a785133f2bff12e7d563acc96684815294d9381874a8e373aca3e2224667b576e87b14467761f1@[::]:30303`

    - Download `/home/<user>/genesis.json` file

- Create a new Ubuntu 16.04 VM in the same VNET and Transaction node subnet

- SSH into the new VM and install geth

    - https://geth.ethereum.org/install/#install-on-ubuntu-via-ppas

- Configure geth on the new VM

    - `geth init genesis.json`

    - update `static-nodes.json` file and upload it to `~/.ethereum/`

    - `geth --networkid 999999 --nodiscover --verbosity 5 console`

- Verfiy new VM is connected to network 

    - `geth attach`

    - `personal.newAccount()`  and copy the account address

    - `eth.getBalance(eth.accounts[0])`

    -  Open the etherem admin app in the browser using the load balancer ip address of the ethereum cluster (not the VM)

    - Send 1000 ether to the new account address created above

    - `eth.getBalance(eth.accounts[0])`



