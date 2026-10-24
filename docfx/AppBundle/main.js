import { dotnet } from './_framework/dotnet.js';

async function initialize() {
    const canvas = document.getElementById('canvas');
    const params = new URLSearchParams(window.location.search);
    let lightTheme = params.get('theme') !== 'dark';
    let setRuntimeTheme;
    const applyTheme = () => {
        document.documentElement.classList.toggle('dark-theme', !lightTheme);
        setRuntimeTheme?.(lightTheme);
    };
    // Register before loading .NET so a theme change during startup is retained.
    window.addEventListener('message', event => {
        if (event.origin !== window.location.origin || event.source !== window.parent) return;
        if (event.data?.type !== 'jitter-theme' || !['light', 'dark'].includes(event.data.theme)) return;
        lightTheme = event.data.theme === 'light';
        applyTheme();
    });
    applyTheme();

    // Force an opaque WebGL backbuffer so browser page colors cannot bleed
    // through anti-aliased canvas content such as UI text.
    const originalGetContext = canvas.getContext.bind(canvas);
    canvas.getContext = function(type, attrs) {
        if (type === 'webgl' || type === 'webgl2' || type === 'experimental-webgl') {
            return originalGetContext(type, {
                ...attrs,
                alpha: false
            });
        }

        return originalGetContext(type, attrs);
    };

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

    function mainLoop() {
        exports.WebDemo.Application.UpdateFrame();
        window.requestAnimationFrame(mainLoop);
    }

    function queuePointerTap(clientX, clientY) {
        const point = pointerToCanvasPixels(clientX, clientY);
        exports.WebDemo.Application.PointerTap(point.x, point.y);
    }

    canvas.addEventListener('keydown', evt => {
        if (evt.key === 'ArrowLeft' || evt.key === 'ArrowRight') evt.preventDefault();
    });
    canvas.addEventListener('pointerdown', evt => {
        canvas.focus({ preventScroll: true });
        queuePointerTap(evt.clientX, evt.clientY);
        if (evt.pointerType === 'touch') {
            evt.preventDefault();
        }
    }, { passive: false });

    await runMain();

    setRuntimeTheme = value => exports.WebDemo.Application.SetTheme(value);
    applyTheme();
    // The bundled raylib web backend uses an 800 × 600 logical viewport.
    // Scale it with CSS: changing only the backing buffer crops the scene and UI.
    canvas.width = 800;
    canvas.height = 600;

    document.getElementById('spinner')?.remove();
    window.requestAnimationFrame(mainLoop);
}

initialize().catch(err => {
    console.error('An error occurred during initialization:', err);
    const spinner = document.getElementById('spinner');
    if (spinner) {
        spinner.replaceChildren();
        spinner.setAttribute('role', 'alert');
        const message = document.createElement('p');
        message.textContent = 'The playground could not load. Please reload to try again.';
        const retry = document.createElement('button');
        retry.type = 'button';
        retry.textContent = 'Reload playground';
        retry.addEventListener('click', () => window.location.reload());
        spinner.append(message, retry);
    }
});
