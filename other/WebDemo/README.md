## Jitter2 in the browser

**Credits for raylib-cs in the browser and this README go to: https://github.com/disketteman/DotnetRaylibWasm and https://github.com/Kiriller12/RaylibWasm**

## Setup

Make sure you have the latest version of .NET 9.

Install the official wasm tooling:

```
dotnet workload install wasm-tools
dotnet workload install wasm-experimental
```

Install a tool to create ad-hoc http server to serve `application/wasm`:

```
dotnet tool install --global dotnet-serve
```

## Run it

`publish` the solution. Don't use `build`. Publishing may take a while.

```
dotnet publish -c Release
```

To serve the files use this command:

```
dotnet serve --mime .wasm=application/wasm --mime .js=text/javascript --mime .json=application/json --directory ./bin/Release/net9.0/browser-wasm/AppBundle
```
