# Overview

This project contains multiple client as well as a server application which sole goal is to collect system symbols of the device in which it runs. Nothing besides operating system images are collected.

# Motivation

It is often not possible or desired to symbolicate crashes on the device which had the crash. One of the cases is when the application didn't include debug information with its binaries. 
In order to process crashes outside of the device, the symbols are required in the backend to allow symbolication. Sometimes vendors make their symbols available publicly in a _symbol server_ like Microsoft Symbol Server but sometimes vendors just don't.
This project is an attemp to reduce friction to acquire system images from different devices so that we can build a database of system images.

# Privacy

This project doesn't collect any personal identifiable information. It will the device's system images and also some _device information_ like CPU architecture, model, etc solely to distinguish which devices have which symbols. 

# How does it work?

When consent is given by the user, the client application will communicate with the backend to register the device.
The backend only accepts system images from devices registered prior to uploading anything.

Once the device registers itself, it enumerates the system images (for example in `/lib`/ or `/system/lib`), verifies if it has debug information or stack unwinding and only if it does, it calls the backend with the system image `debug id`. The server will respond if that image is wanted or not which tells the client application whether to send it or not.

