import { dotnet } from './_framework/dotnet.js'

const { getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

dotnet.instance.Module['canvas'] = document.getElementById('canvas');

function mainLoop() {
    exports.WebDemo.Application.UpdateFrame();

    window.requestAnimationFrame(mainLoop);
}

// https://github.com/dotnet/runtime/blob/74c608d67d64675ff840c5888368669777c8aa2c/src/mono/wasm/templates/templates/browser/wwwroot/main.js#L31-L32
// run the C# Main() method and keep the runtime process running and executing further API calls
await runMain();

// We're ready to dotnet.run, so let's remove the spinner
const loading_div = document.getElementById('spinner');
loading_div.remove();

window.requestAnimationFrame(mainLoop);