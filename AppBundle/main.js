import { dotnet } from './_framework/dotnet.js';

async function initialize() {
    const canvas = document.getElementById('canvas');

    const { getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .create();

    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);

    dotnet.instance.Module['canvas'] = canvas;

    function pointerToCanvasPixels(clientX, clientY) {
        const rect = canvas.getBoundingClientRect();
        const localX = clientX - rect.left;
        const localY = clientY - rect.top;
        const scaleX = rect.width > 0 ? canvas.width / rect.width : 1;
        const scaleY = rect.height > 0 ? canvas.height / rect.height : 1;

        return {
            x: localX * scaleX,
            y: localY * scaleY
        };
    }

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

    function queuePointerTap(clientX, clientY) {
        const point = pointerToCanvasPixels(clientX, clientY);
        exports.WebDemo.Application.PointerTap(point.x, point.y);
    }

    window.addEventListener('resize', resizeCanvasToDisplaySize);
    window.addEventListener('orientationchange', resizeCanvasToDisplaySize);
    canvas.addEventListener('pointerdown', evt => {
        queuePointerTap(evt.clientX, evt.clientY);
        if (evt.pointerType === 'touch') {
            evt.preventDefault();
        }
    }, { passive: false });

    await runMain();

    resizeCanvasToDisplaySize();

    document.getElementById('spinner')?.remove();
    window.requestAnimationFrame(mainLoop);
}

initialize().catch(err => {
    console.error('An error occurred during initialization:', err);
});
