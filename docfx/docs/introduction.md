# Welcome

Jitter2 is a fast and lightweight physics engine for .NET.
This documentation contains tutorials and reference material.
Work in progress—contributions are welcome.

<div class="demo-shell">
  <div class="demo-card" id="demo_card">
    <div class="demo-copy">
      <strong>Interactive Web Demo</strong>
      <span>Load a simple demo in the browser.</span>
    </div>
    <button class="demo-button" id="load_demo_button" type="button">Load Demo</button>
  </div>
  <div class="demo-frame-wrap" id="demo_frame_wrap" hidden>
    <iframe
      id="demo_frame"
      title="Jitter2 Web Demo"
      loading="lazy"
      allow="fullscreen"
      referrerpolicy="strict-origin-when-cross-origin"></iframe>
  </div>
</div>

<style>
.demo-shell {
  margin: 1.5rem 0 0;
}

.demo-card,
.demo-frame-wrap {
  width: 100%;
  aspect-ratio: 800 / 600;
  border-radius: 10px;
  overflow: hidden;
  background: linear-gradient(160deg, #15181d 0%, #1f2732 100%);
  border: 1px solid rgba(255, 255, 255, 0.08);
}

html[data-bs-theme="light"] .demo-card,
html[data-bs-theme="light"] .demo-frame-wrap {
  background: linear-gradient(160deg, #eef3f8 0%, #dde8f2 100%);
  border-color: rgba(34, 48, 60, 0.12);
}

.demo-card {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  padding: 1.5rem;
  text-align: center;
}

.demo-copy {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  max-width: 34rem;
  align-items: center;
}

.demo-copy strong {
  font-size: 1.1rem;
  color: #fff;
}

html[data-bs-theme="light"] .demo-copy strong {
  color: #22303c;
}

.demo-copy span {
  color: #b6c2d0;
  line-height: 1.5;
}

html[data-bs-theme="light"] .demo-copy span {
  color: #5f6f80;
}

.demo-button {
  appearance: none;
  border: 0;
  border-radius: 999px;
  padding: 0.75rem 1.2rem;
  background: #5ba8f5;
  color: #08111c;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.demo-button:hover,
.demo-button:focus-visible {
  background: #7cbaf8;
}

html[data-bs-theme="light"] .demo-button {
  background: #22303c;
  color: #f4f8fb;
}

html[data-bs-theme="light"] .demo-button:hover,
html[data-bs-theme="light"] .demo-button:focus-visible {
  background: #314253;
}

.demo-frame-wrap iframe {
  display: block;
  width: 100%;
  height: 100%;
  border: 0;
}

@media (max-width: 700px) {
  .demo-card {
    padding: 1.1rem;
  }
}
</style>

<script>
(() => {
  const button = document.getElementById('load_demo_button');
  const card = document.getElementById('demo_card');
  const frameWrap = document.getElementById('demo_frame_wrap');
  const frame = document.getElementById('demo_frame');

  if (!button || !card || !frameWrap || !frame) {
    return;
  }

  const buildFrameUrl = () => {
    const theme = document.documentElement.getAttribute('data-bs-theme') === 'light'
      ? 'light'
      : 'dark';

    return `https://jitterphysics.com/AppBundle/index.html?embed=1&theme=${theme}`;
  };

  button.addEventListener('click', () => {
    if (!frame.src) {
      frame.src = buildFrameUrl();
    }

    card.hidden = true;
    frameWrap.hidden = false;
  }, { once: true });

  new MutationObserver(() => {
    if (frame.src) {
      frame.src = buildFrameUrl();
    }
  }).observe(document.documentElement, {
    attributes: true,
    attributeFilter: ['data-bs-theme']
  });
})();
</script>
