#!/bin/bash

echo "Adding CosmosDb emulator self-signed certificate..."

add_cert(){
  curl --connect-timeout 2 -k https://cosmosdb:8081/_explorer/emulator.pem >~/emulator.pem
  status=$?
  [ $status -eq 0 ] \
    && echo "CosmosDb is running, updating certificate store." \
    && cp ~/emulator.pem /usr/local/share/ca-certificates/emulator.pem \
    && update-ca-certificates
}

until add_cert >> /dev/null 2>&1
do
  sleep 2s
  echo "Waiting for CosmosDb emulator to be available..."
done

echo "CosmosDb emulator self-signed certificate added successfully."

