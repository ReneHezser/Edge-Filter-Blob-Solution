## Troubleshooting

- "Please set up iotedgehubdev" message keeps showing. Setting them up via GUI didn't work for me. Setting in the console with *iotedgehubdev setup -c 'xyz'* did.

- Running on Windows will cause problems with the blob storage mount: [permission error](https://docs.microsoft.com/en-us/answers/questions/649865/iotedge-blob-storage-local-can-create-container-bu.html)

- setting permissions on Linux: [setting permissions on the mapped folder](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-store-data-blob?view=iotedge-2020-11#granting-directory-access-to-container-user-on-linux) (the devcontainer will do this for you)
    ```bash
    sudo mkdir /opt/containerdata
    sudo chown -R 11000:11000 /opt/containerdata
    sudo chmod -R 700 /opt/containerdata
    ```
