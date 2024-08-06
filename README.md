<img src="docs/jpeg_ls_logo.png" alt="JPEG-LS Logo" width="100"/>

# CharLS.Native .NET

[![License](https://img.shields.io/badge/License-BSD%203--Clause-blue.svg)](https://raw.githubusercontent.com/team-charls/charls-dotnet/main/LICENSE.md)
[![Build and Test](https://github.com/team-charls/charls-dotnet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/team-charls/charls-dotnet/actions/workflows/dotnet.yml)

CharLS .NET is an C# JPEG-LS library implementation.  
JPEG-LS (ISO-14495-1) is a lossless/near-lossless compression standard for continuous-tone images.

## IMPORTANT : WORK IN PROGRESS

This project is considered work in progress and NOT ready for general use. The following functionality is planned / implemented:

* [X] Decode 8 bit images, interleave mode: none
* [X] Decode 8 bit images, interleave mode: line
* [X] Decode 8 bit images, interleave mode: sample
* [ ] Decode 16 bit images, interleave mode: none
* [ ] Decode 16 bit images, interleave mode: line
* [ ] Decode 16 bit images, interleave mode: sample
* [ ] Encode 8 bit images, interleave mode: none
* [ ] Encode 8 bit images, interleave mode: line
* [ ] Encode 8 bit images, interleave mode: sample
* [ ] Encode 16 bit images, interleave mode: none
* [ ] Encode 16 bit images, interleave mode: line
* [ ] Encode 16 bit images, interleave mode: sample
* [ ] General code clean-up
* [ ] Performance tuning
* [ ] Release NuGet package

## Features

* .NET 8.0 class library.
* Support for the .NET platforms: Windows, Linux and macOS.

## How to use

CharLS.Managed can be added to your C# project using the dotnet command line or the NuGet Package Manager in Visual Studio.

### Install using the dotnet command line

```bash
dotnet add package CharLS.Managed
```

### How to use the C# classes in the NuGet package

A sample application is included in the GitHub repository that demonstrates how to convert common image types like .bmp, .png and .jpg to .jls (JPEG-LS).

## General steps to build this repository

* Use Git to get a clone of this repository:  

```bash
 git clone https://github.com/team-charls/charls-dotnet.git
```

* Use the .NET 8.0 CLI or Visual Studio 2022 (v17.10 or newer) to build the solution file CharLSDotNet.sln. For example: `dotnet build && dotnet test && dotnet publish` to build the nuget package.

### Building Windows DLLs and code signing all components

Building the NuGet package with signed Windows DLLs can only be done on the Window platform with Visual Studio 2022 or with Build tools for Visual Studio 2022.
To support code signing with a code signing certificate, stored on a smart card, a Windows command file is available: `create-signed-nuget-package.cmd`.
Instructions:

* Open a Visual Studio Developer Command Prompt
* Go the root of the cloned repository
* Ensure the code signing certificate is available
* Execute the command `create-signed-nuget-package.cmd certificate-thumb-print time-stamp-url`  
 The certificate thumbprint and time stamp URL arguments are depending on the used code signing certificate.

 All DLLs and the NuGet package itself will be signed.

## About the JPEG-LS image compression standard

More information about JPEG-LS can be found in the [README](https://github.com/team-charls/charls/blob/master/README.md) from the C++ CharLS project.
