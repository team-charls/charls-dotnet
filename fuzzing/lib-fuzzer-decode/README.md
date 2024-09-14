# Instructions how to fuzz

LibFuzzerDecode uses SharpFuzz as fuzzing framework.
General document can be found at: <https://github.com/Metalnem/sharpfuzz>

## Windows

### Initial setup

- Download and install the SharpFuzz .NET tool:  
  dotnet tool install --global SharpFuzz.CommandLine

- Download libfuzzer-dotnet-windows.exe and put it somewhere on the PATH.

### Running fuzzing tests

- Open a PowerShell prompt
- Go to the folder with the LibFuzzerDecode.csproj
- Execute the script fuzz-libfuzzer.ps1  -libFuzzer "libfuzzer-dotnet-windows.exe"  -project  FuzzDecode.csproj -corpus .\test-cases\
