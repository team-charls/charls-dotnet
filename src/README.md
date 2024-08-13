# CharLS.Managed .NET

CharLS .NET is a C# JPEG-LS library implementation.  
JPEG-LS (ISO-14495-1) is a lossless/near-lossless compression standard for continuous-tone images.

## Features

* .NET 8.0 class library.
* Support for the .NET platforms: Windows, Linux and macOS.

## How to use

CharLS.Native can be added to your C# project using the dotnet command line or the NuGet Package Manager in Visual Studio.

### Install using the dotnet command line

```bash
dotnet add package CharLS.Managed
```

### How to use the C# classes in the NuGet package

A sample application is included in the GitHub repository that demonstrates how to convert common image types like .bmp, .png and .jpg to .jls (JPEG-LS).

## About the JPEG-LS image compression standard

More information about JPEG-LS can be found in the [README](https://github.com/team-charls/charls/blob/master/README.md) from the C++ CharLS project.
This repository also contains instructions how the build the native C++ CharLS shared library from source.
