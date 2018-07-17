# Raspberry Pi as Ethereum Node

#### Using [ganache](https://nethereum.readthedocs.io/en/latest/ethereum-and-clients/ganache-cli/)

- `npm install -g ganache-cli`
- `ganache-cli --secure -u 0 -h <raspberry pi IP address>`


#### Using geth

- Install Ethereum
    - `wget https://gethstore.blob.core.windows.net/builds/geth-linux-arm7-1.8.12-37685930.tar.gz`
    - `tar zxvf geth-linux-arm7-1.8.12-37685930.tar.gz`
    - `cd geth-linux-arm7-1.8.12-37685930`
    - `sudo cp geth /usr/local/bin`
    - `geth version`

- Configure and Start Ethereum Private Network
    - `geth account new` and copy the account address
    - update the `alloc` address in `genesis.json` with the account address copied above
    - `geth init genesis.json`
    - `geth --networkid 112233 --rpc --rpccorsdomain "*" --verbosity 3 console`