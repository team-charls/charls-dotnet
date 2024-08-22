<img src="docs/jpeg_ls_logo.png" alt="JPEG-LS Logo" width="100"/>

# CharLS .NET

[![License](https://img.shields.io/badge/License-BSD%203--Clause-blue.svg)](https://github.com/team-charls/charls-dotnet/blob/main/LICENSE.md)
[![Build and Test](https://github.com/team-charls/charls-dotnet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/team-charls/charls-dotnet/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/team-charls/charls-dotnet/badge.svg)](https://coveralls.io/github/team-charls/charls-dotnet)
[![NuGet](https://img.shields.io/nuget/v/CharLS.Managed.svg)](https://www.nuget.org/packages/CharLS.Managed)

CharLS .NET is a C# JPEG-LS library implementation.  
JPEG-LS (ISO-14495-1) is a lossless/near-lossless compression standard for continuous-tone images.

## Features

* .NET 8.0 class library.
* Support for the .NET platforms: Windows, Linux and macOS.

## Performance

A couple of benchmarks have been executed to assist to decide which codec to use.
The following codecs were measured:

|Codec         |Version|Release date|Description|
|------------  |-------|------------|----------------------------------------------|
|CharLS.Managed| 0.8.0 |2024-08-23  |C# implementation                             |
|CharLS.Native | 3.2.0 |2023-09-24  |C# wrapper around C++ implementation          |
|cscharls      | 0.1.1 |2017-04-24  |C# implementation                             |
|charls-js     | 2.1.1 |2021-10-07  |WebAssembly implementation based on C++ source|

and the following test images were used:

|Image name    |Width  |Height|Bits per sample|Components|Description               |
|------------  |------:|-----:|--------------:|---------:|--------------------------|
|Tulips        |    512|   512|              8|         1|Monochrome image of tulips|
|MG1           |   3064|  4664|             12|         1|Monochrome medical image  |
|Delta E       |   3072|  2048|              8|         3|Artificial RGB image      |

The benchmarks were performed on 2 hardware platforms: x86 and ARM64.  
The column 'NEAR' defines the allowed error, 0 means lossless encoding.  
The column 'ILV' defines the interleave mode used to encode the image.

### x86-64 Platform

Characteristics of the test environment: AMD Ryzen 9 5950X, .NET 8.0.8 (X64 RyuJIT AVX2)  
WebAssembly engine: Chrome V8 12.8.374.21

|Operation      |NEAR|ILV   |CharLS.Managed|CharLS.Native|cscharls |charls-js|
|---------------|---:|------|-------------:|------------:|--------:|--------:|
|Decode Tulips  |   0|None  |        5.0 ms|       3.6 ms|  10.4 ms|   5.6 ms|
|Decode MG1     |   0|None  |      292.3 ms|     205.0 ms| 676.3 ms| 335.2 ms|
|Decode Delta E |   0|Sample|       80.0 ms|      89.4 ms|        -|  89.5 ms|
|Encode Tulips  |   0|None  |        7.1 ms|       4.7 ms|  11.8 ms|        -|
|Encode MG1     |   0|None  |      404.3 ms|     256.3 ms| 695.8 ms|        -|

## Arm64 Platform

Characteristics of the test environment: Snapdragon Compute Platform, .NET 8.0.8 (Arm64 RyuJIT AdvSIMD)  
WebAssembly engine: Chrome V8 12.8.374.21

|Operation      |NEAR|ILV   |CharLS.Managed|CharLS.Native|cscharls |charls-js|
|---------------|---:|------|-------------:|------------:|--------:|--------:|
|Decode Tulips  |   0|None  |        7.2 ms|       4.7 ms|  14.3 ms|  10.5 ms|
|Decode MG1     |   0|None  |      431.7 ms|     261.8 ms|1019.4 ms| 542.1 ms|
|Encode Tulips  |   0|None  |        9.6 ms|       5.5 ms|  15.7 ms|        -|
|Encode MG1     |   0|None  |      564.0 ms|     318.6 ms|1017.2 ms|        -|

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

* Use the .NET 8.0 CLI or Visual Studio 2022 (v17.11 or newer) to build the solution file CharLSDotNet.sln.  
 For example: `dotnet build && dotnet test && dotnet publish` to build the NuGet package.  
 Building can be done on all supported .NET SDK platforms: Windows, Linux or macOS

### Code signing the assembly and the NuGet package

Building the NuGet package with a signed assembly DLL and NuGet package can only be done
on the Window platform with Visual Studio 2022 or with Build tools for Visual Studio 2022.
To support code signing with a code signing certificate, stored on a smart card, a
Windows command file is available: `create-signed-nuget-package.cmd`.
Instructions:

* Open a Visual Studio Developer Command Prompt
* Go the root of the cloned repository
* Ensure the code signing certificate is available
* Execute the command `create-signed-nuget-package.cmd certificate-thumb-print time-stamp-url`  
 The certificate thumbprint and time stamp URL arguments are depending on the used code signing certificate.

All assembly DLLs and the NuGet package itself will be signed.

## About the JPEG-LS image compression standard

More information about JPEG-LS can be found in the [README](https://github.com/team-charls/charls/blob/master/README.md)
from the C++ CharLS project.
