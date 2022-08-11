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

## Warning about the TLS certificate expiration by March 2023

The demonstrated usage of the Baltimore certificate for TLS security needs your attention.

This Baltimore certificate will expire eventually. This application will stop functioning after that expiration date due to the pinning of the certificate.

See [this site](https://techcommunity.microsoft.com/t5/internet-of-things-blog/azure-iot-tls-critical-changes-are-almost-here-and-why-you/ba-p/2393169?WT.mc_id=IoT-MVP-5002324) for more background information.

This sample application lacks:

- Supporting another certificate while the baltimore certificate is deprecated soon
