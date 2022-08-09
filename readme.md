# Demonstration of Azure IoT Hub access without SDK

## Disclaimer

This repo is just a demonstration regarding the logic needed to communicate against the (limited) MQTT broker capabilities of the Azure IoT Hub.

This demonstration is not feature complete or production ready.

I personally recommend to use the Azure IoT Device SDKs if possible. 

These are production ready, production tested by many users and devices are resilient against many issues that can occur in the lifetime of an IoT device.

## Credits

This repo is inspired by this [blog post](https://www.petecodes.co.uk/connecting-a-raspberry-pi-pico-w-to-microsoft-azure-iot-hub-using-micropython-and-mqtt/) written by my friend Pete Gallagher @pete_codes.

## Demonstrated logic

The following logic is demonstrated

- Connecting the device to the MQTT broker as part of the IoT Hub
- Sending device messages
- Sending user properties alongside of device messages
- Receiving Direct Methods as event, and responding to it
- Receiving cloud messages as event
- Getting cloud messages on startup, being sent while the device was offline
- Receiving user properties alongside cloud messages
- Receiving desired properties as event
- Reading desired properties at the start of the application
- Sending Reported properties

## Missing logic

The following logic is missing:

- Supporting another certificate because the baltimore certificate is deprecated soon
- Creating the SAS token on the device itself so it can be refresh once the current one expires.
- Everything else I forgot to research

## Links

### Docs

https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support

### Samples

https://github.com/Azure-Samples/IoTMQTTSample/blob/master/src/Windows/PnPMQTTWin32/PnPMQTTWin32.cpp
