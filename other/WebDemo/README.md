## Jitter2 in the browser

**Credits for raylib-cs in the browser and this README go to: https://github.com/disketteman/DotnetRaylibWasm and https://github.com/Kiriller12/RaylibWasm**

## Setup

The web demo uses the Jitter2 2.13.0 NuGet package, pinned in `WebDemo.csproj`.

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

## Appearance and documentation bundle

The playground defaults to a light theme. Use `?theme=dark` for the dark variant,
or `?embed=1` to hide the standalone page header and instructions. The canvas
keeps raylib's 800 × 600 logical viewport and scales with CSS so the entire scene
and all five tabs remain visible at smaller widths. The landing page follows the
documentation's Light / Dark / Auto preference and updates the embedded demo's
theme in place, preserving the current scene.

The documentation serves a prebuilt copy from `docfx/AppBundle`. After publishing
changes, synchronize the generated AppBundle (including `_framework`) into that
directory before building the documentation. Keep `index.html` and `main.js` in
sync with this project's sources.
