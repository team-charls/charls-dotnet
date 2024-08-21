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

|Name          |Version|Release Date|
|------------  |-------|------------|
|CharLS.Managed| 0.8.0 |2024-08-22  |
|CharLS.Native | 3.2.0 |2023-09-24  |
|cscharls      | 0.1.1 |2017-04-24  |
|charls-js     | 2.1.1 |2021-10-07  |

## x86-64 Platform

Characteristics of the test environment: AMD Ryzen 9 5950X, .NET 8.0.8 (X64 RyuJIT AVX2)

|Operation                                         |CharLS.Managed|CharLS.Native|cscharls |charls-js|
|--------------------------------------------------|-------------:|------------:|--------:|--------:|
|Decode Tulips  (512x512x8x1 lossless)             |        5.0 ms|       3.6 ms|  10.4 ms|   5.6 ms|
|Decode MG1     (3064x4664x12x1 lossless)          |      292.3 ms|     205.0 ms| 676.3 ms| 335.2 ms|
|Decode Delta E (3072x2048x8x3 lossless ilv-sample)|       80.0 ms|      89.4 ms|        -|  89.5 ms|
|Encode Tulips  (512x512x8x1 lossless)             |        7.1 ms|       4.7 ms|  11.8 ms|        -|
|Encode MG1     (3064x4664x12x1 lossless)          |      404.3 ms|     256.3 ms| 695.8 ms|        -|

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
 Building can be done on all supported .NET SDK platforms: Windows, Linux, macOS

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
