import { dotnet } from './_framework/dotnet.js';

async function initialize() {
    const canvas = document.getElementById('canvas');

    const { getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .create();

    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);

    dotnet.instance.Module['canvas'] = canvas;

    function resizeCanvasToDisplaySize() {
        const dpr = Math.min(window.devicePixelRatio || 1, 2);

        const cssWidth = canvas.clientWidth;
        const cssHeight = canvas.clientHeight;

        const pixelWidth = Math.max(1, Math.round(cssWidth * dpr));
        const pixelHeight = Math.max(1, Math.round(cssHeight * dpr));

        if (canvas.width !== pixelWidth || canvas.height !== pixelHeight) {
            canvas.width = pixelWidth;
            canvas.height = pixelHeight;
        }

        exports.WebDemo.Application.Resize(pixelWidth, pixelHeight);
    }

    function mainLoop() {
        resizeCanvasToDisplaySize();
        exports.WebDemo.Application.UpdateFrame();
        window.requestAnimationFrame(mainLoop);
    }

    window.addEventListener('resize', resizeCanvasToDisplaySize);
    window.addEventListener('orientationchange', resizeCanvasToDisplaySize);

    await runMain();

    resizeCanvasToDisplaySize();

    document.getElementById('spinner')?.remove();
    window.requestAnimationFrame(mainLoop);
}

initialize().catch(err => {
    console.error('An error occurred during initialization:', err);
});
