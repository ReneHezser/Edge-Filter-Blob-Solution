# IoT Edge Filter Solution

Filter messages sent to an IoT edge hub and send them to a (local) blob storage.

## Description

This sample shows how to use custom code in a module to use message routing from another module (a simulated temperature sensor in this case), analyse the incoming messages and send them to a local blob storage module.

### Architecture

The solution contains of three containers (IoT edge modules).

1. [Simulated Temperature](https://azuremarketplace.microsoft.com/en-us/marketplace/apps/azure-iot.simulated-temperature-sensor?tab=Overview)
2. this custom filter module
3. [Blob Storage on IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-store-data-blob?view=iotedge-2020-11)

To be precise, two additional containers are automatically started by IoT Edge

4. [edgeAgent](https://github.com/Azure/iotedge/tree/main/edge-agent)
5. [edgeHub](https://github.com/Azure/iotedge/tree/main/edge-hub)

### Development / VSCode project

To simplify the development, I used templates, a devcontainer and GitHub actions. Even debugging is possible with dev tools.

- [Azure IoT Tools for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=vsciot-vscode.azure-iot-tools) provide templates for IoT Edge Modules in various programing languages
- [development containers](https://code.visualstudio.com/docs/remote/create-dev-container) enable you to develop in a controlled environment, which is setup automatically without dependencies to the development machine (at least most of them)
- [Build and push Docker images](https://github.com/marketplace/actions/build-and-push-docker-images) (together with other actions) build the container image
- [Azure IoT EdgeHub Dev Tool](https://github.com/Azure/iotedgehubdev) provide the debugging experience without the need to deploy the module to another edge device first

#### env file

To build the solution locally with the IoT Tools from VSCode, create an ```.env``` file with some values. They will be used to replace variables in the deployment manifest template to genereate the actual deployment manifest (that will be saved to the ```config``` folder.)

```bash
EDGE_STORAGE_PATH=/opt/containerdata
EDGE_STORAGE_ACCOUNT_KEY=your 64-byte base64 key
CLOUD_STORAGEACCOUNT_CONNECTIONSTRING="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
```

*You can use [GeneratePlus](https://generate.plus/en/base64) to create a storage key (use 16 as length).*

*The storage key will also be passed to the filter module to be able to write blobs to it.*

## Known issues

- Running the solution in the IoT Edge Simulator will not upload the blobs to the connected Azure Storage Account (local access via Storage SDK is working)

## Troubleshooting

- "Please set up iotedgehubdev" message keeps showing. Setting them up via GUI didn't work for me. Setting in the console with *iotedgehubdev setup -c 'xyz'* did.

- Running on Windows will cause problems with the blob storage mount: [permission error](https://docs.microsoft.com/en-us/answers/questions/649865/iotedge-blob-storage-local-can-create-container-bu.html)

- setting permissions on Linux: [setting permissions on the mapped folder](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-store-data-blob?view=iotedge-2020-11#granting-directory-access-to-container-user-on-linux) 

    The devcontainer setup of this solution will do this for you. See [this line](https://github.com/ReneHezser/Edge-Filter-Blob-Solution/blob/main/.devcontainer/devcontainer.json#L61).

    ```bash
    sudo mkdir /opt/containerdata
    sudo chown -R 11000:11000 /opt/containerdata
    sudo chmod -R 700 /opt/containerdata
    ```