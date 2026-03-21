# How to create a release

## Create NuGet packages

- Open Visual Studio Developer Command Prompt
- Change directory to the root directory of this project
- Run the following command
```shell
dotnet pack -c Release -o release DbConnectionPlus.slnx
```
