# Design Notes

## NuGet Package

There are a couple of things that need to be done, that make creating a NuGet package not standard:

- Code signing of all DLLs before the package is created. The private key is stored on a smart card and
signing cannot be done in the CI pipeline.

- Native DLLs need to be put into the NuGet package.

