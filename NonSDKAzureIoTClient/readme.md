# Non SDK Azure IoT client

## Device Connection settings

You need to add a connection settings using a file named appsettings.json:

```
{
    "Settings": {
        "hostName": "[your iot hub name].azure-devices.net",
        "clientId": "[your device name]",
        "sasSig": "[GUID part of the SAS token you need to generate]",
        "sasSe": "[Expiration int of the SAS token you need to generate]"
    }
}
```

This file is ignored by github check-in using .gitignore
