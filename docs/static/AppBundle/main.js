import { dotnet } from './_framework/dotnet.js';

async function initialize() {
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

    // Run the C# Main() method and keep the runtime process running and executing further API calls
    await runMain();

    // Remove the spinner once the application is ready
    const loading_div = document.getElementById('spinner');
    loading_div.remove();

    window.requestAnimationFrame(mainLoop);
}

// Initialize the application
initialize().catch(err => {
    console.error('An error occurred during initialization:', err);
});
