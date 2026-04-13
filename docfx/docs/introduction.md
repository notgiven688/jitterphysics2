---
_disableBreadcrumb: true
_disableContribution: true
---

<div class="jp-hero">
  <p class="jp-hero-eyebrow">Fast &middot; Lightweight &middot; Pure C# &middot; MIT License</p>
  <h1 class="jp-hero-title">Jitter Physics 2</h1>
  <p class="jp-hero-sub">
    An impulse-based 3D rigid-body physics engine for .NET &mdash;<br>
    zero native dependencies, runs anywhere .NET runs.
  </p>
  <div class="jp-hero-ctas">
    <a href="~/docs/tutorials/boxes/project-setup.md" class="jp-btn-primary">Get Started</a>
    <a href="https://github.com/notgiven688/jitterphysics2" class="jp-btn-ghost">View on GitHub</a>
  </div>
  <div class="jp-badges">
    <img src="https://img.shields.io/nuget/v/Jitter2?style=flat-square&label=NuGet&color=5ba8f5" alt="NuGet version">
    <img src="https://img.shields.io/github/stars/notgiven688/jitterphysics2?style=flat-square&label=Stars&color=5ba8f5" alt="GitHub stars">
    <img src="https://img.shields.io/badge/.NET-8%2B-blueviolet?style=flat-square" alt=".NET 8+">
    <img src="https://img.shields.io/github/license/notgiven688/jitterphysics2?style=flat-square&color=22c55e" alt="MIT License">
  </div>
</div>

<div class="jp-features">
  <div class="jp-feature-card">
    <div class="jp-feature-icon">&#9889;</div>
    <div class="jp-feature-title">Zero Dependencies</div>
    <div class="jp-feature-body">Pure C# with no native DLLs, no P/Invoke. Runs on any platform that supports .NET 8 or later.</div>
  </div>
  <div class="jp-feature-card">
    <div class="jp-feature-icon">&#128296;</div>
    <div class="jp-feature-title">Impulse-Based Dynamics</div>
    <div class="jp-feature-body">Semi-implicit Euler integrator with sub-stepping for stable, stiff simulations at interactive frame rates.</div>
  </div>
  <div class="jp-feature-card">
    <div class="jp-feature-icon">&#129513;</div>
    <div class="jp-feature-title">Deterministic Solver</div>
    <div class="jp-feature-body">Optional cross-platform deterministic mode for reproducible simulations.</div>
  </div>
  <div class="jp-feature-card">
    <div class="jp-feature-icon">&#128164;</div>
    <div class="jp-feature-title">Massive Sleeping Worlds</div>
    <div class="jp-feature-body">Deactivation system keeps large simulations fast — 100k+ inactive bodies have near-zero per-frame cost.</div>
  </div>
  <div class="jp-feature-card">
    <div class="jp-feature-icon">&#127919;</div>
    <div class="jp-feature-title">Speculative Contacts</div>
    <div class="jp-feature-body">Prevents tunneling at high velocities without the overhead of continuous collision detection.</div>
  </div>
  <div class="jp-feature-card">
    <div class="jp-feature-icon">&#128279;</div>
    <div class="jp-feature-title">Rich Constraints</div>
    <div class="jp-feature-body">BallSocket, Hinge, Prismatic, Universal Joint, Linear &amp; Angular Motor, Spring, Distance Limit, and more.</div>
  </div>
  <div class="jp-feature-card">
    <div class="jp-feature-icon">&#128142;</div>
    <div class="jp-feature-title">Convex &amp; Mesh Shapes</div>
    <div class="jp-feature-body">Box, Capsule, Sphere, Cone, Cylinder, ConvexHull, PointCloud, and TriangleMesh &mdash; unified GJK/MPR pipeline.</div>
  </div>
  <div class="jp-feature-card">
    <div class="jp-feature-icon">&#128260;</div>
    <div class="jp-feature-title">Float or Double Precision</div>
    <div class="jp-feature-body">Compile-time precision switch &mdash; the same API and source code works with either <code>float</code> or <code>double</code>.</div>
  </div>
  <div class="jp-feature-card">
    <div class="jp-feature-icon">&#129527;</div>
    <div class="jp-feature-title">Soft-Body Dynamics</div>
    <div class="jp-feature-body">Cloth, ropes, and deformable bodies coexist alongside rigid bodies in the same simulation world.</div>
  </div>
</div>


<div class="demo-shell">
  <div class="demo-card" id="demo_card">
    <div class="demo-copy">
      <strong>Interactive Web Demo</strong>
      <span>Run the physics engine live in your browser &mdash; no install required.</span>
    </div>
    <button class="demo-button" id="load_demo_button" type="button">Launch Demo</button>
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

<div class="jp-nav-cards">
  <a class="jp-nav-card" href="~/docs/tutorials/boxes/project-setup.md">
    <div class="jp-nav-card-icon">&#128218;</div>
    <div class="jp-nav-card-title">Hello World Tutorial</div>
    <div class="jp-nav-card-body">Set up a project and simulate your first falling box in minutes.</div>
  </a>
  <a class="jp-nav-card" href="~/docs/tutorials/teapots/project-setup.md">
    <div class="jp-nav-card-icon">&#127861;</div>
    <div class="jp-nav-card-title">Teapot Stacking</div>
    <div class="jp-nav-card-body">Load convex hull meshes and simulate a tower of Utah teapots.</div>
  </a>
  <a class="jp-nav-card" href="~/docs/documentation/world.md">
    <div class="jp-nav-card-icon">&#128196;</div>
    <div class="jp-nav-card-title">Documentation</div>
    <div class="jp-nav-card-body">Dive into bodies, shapes, constraints, and the broad-phase tree.</div>
  </a>
  <a class="jp-nav-card" href="~/api/Jitter2.yml">
    <div class="jp-nav-card-icon">&#128214;</div>
    <div class="jp-nav-card-title">API Reference</div>
    <div class="jp-nav-card-body">Full generated reference for every public type in Jitter2.</div>
  </a>
</div>

<style>
/* ── Accent & token ─────────────────────────────────────── */
:root { --jp-accent: #5ba8f5; --jp-accent-hover: #7cbaf8; }

/* ── Hero ───────────────────────────────────────────────── */
.jp-hero {
  padding: 2.5rem 0 2rem;
  text-align: center;
}
.jp-hero-eyebrow {
  font-size: 0.8rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--jp-accent);
  margin-bottom: 0.75rem;
}
.jp-hero-title {
  font-size: clamp(2rem, 5vw, 3rem);
  font-weight: 800;
  line-height: 1.1;
  margin: 0 0 1rem;
  color: var(--bs-body-color);
}
.jp-hero-sub {
  font-size: 1.1rem;
  color: var(--bs-secondary-color);
  line-height: 1.7;
  margin-bottom: 1.75rem;
}
.jp-hero-ctas {
  display: flex;
  gap: 0.75rem;
  justify-content: center;
  flex-wrap: wrap;
  margin-bottom: 1.25rem;
}
.jp-btn-primary, .jp-btn-ghost {
  display: inline-block;
  padding: 0.7rem 1.4rem;
  border-radius: 999px;
  font-weight: 700;
  font-size: 0.95rem;
  text-decoration: none;
  transition: background 0.15s, color 0.15s, border-color 0.15s;
}
.jp-btn-primary {
  background: var(--jp-accent);
  color: #08111c;
  border: 2px solid transparent;
}
.jp-btn-primary:hover { background: var(--jp-accent-hover); color: #08111c; text-decoration: none; }
.jp-btn-ghost {
  background: transparent;
  color: var(--bs-body-color);
  border: 2px solid var(--bs-border-color);
}
.jp-btn-ghost:hover {
  border-color: var(--jp-accent);
  color: var(--jp-accent);
  text-decoration: none;
}
.jp-badges {
  display: flex;
  gap: 0.4rem;
  justify-content: center;
  flex-wrap: wrap;
}
.jp-badges img { height: 20px; border-radius: 4px; }

/* ── Feature grid ───────────────────────────────────────── */
.jp-features {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(210px, 1fr));
  gap: 1rem;
  margin: 2.5rem 0;
}
.jp-feature-card {
  background: var(--bs-tertiary-bg);
  border: 1px solid var(--bs-border-color);
  border-radius: 0.75rem;
  padding: 1.25rem 1.1rem 1.1rem;
  transition: border-color 0.15s, transform 0.15s;
}
.jp-feature-card:hover {
  border-color: var(--jp-accent);
  transform: translateY(-2px);
}
.jp-feature-icon {
  font-size: 1.5rem;
  margin-bottom: 0.6rem;
  line-height: 1;
}
.jp-feature-title {
  font-weight: 700;
  font-size: 0.95rem;
  margin-bottom: 0.4rem;
  color: var(--bs-body-color);
}
.jp-feature-body {
  font-size: 0.85rem;
  color: var(--bs-secondary-color);
  line-height: 1.5;
}

/* ── Demo shell ─────────────────────────────────────────── */
.demo-shell { margin: 0 0 2rem; }
.demo-card,
.demo-frame-wrap {
  width: 100%;
  aspect-ratio: 800 / 600;
  border-radius: 10px;
  overflow: hidden;
  background: linear-gradient(160deg, #15181d 0%, #1f2732 100%);
  border: 1px solid rgba(255,255,255,0.08);
}
html[data-bs-theme="light"] .demo-card,
html[data-bs-theme="light"] .demo-frame-wrap {
  background: linear-gradient(160deg, #eef3f8 0%, #dde8f2 100%);
  border-color: rgba(34,48,60,0.12);
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
.demo-copy { display: flex; flex-direction: column; gap: 0.5rem; max-width: 34rem; align-items: center; }
.demo-copy strong { font-size: 1.1rem; color: #fff; }
html[data-bs-theme="light"] .demo-copy strong { color: #22303c; }
.demo-copy span { color: #b6c2d0; line-height: 1.5; }
html[data-bs-theme="light"] .demo-copy span { color: #5f6f80; }
.demo-button {
  appearance: none;
  border: 0;
  border-radius: 999px;
  padding: 0.75rem 1.4rem;
  background: var(--jp-accent);
  color: #08111c;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.15s;
}
.demo-button:hover, .demo-button:focus-visible { background: var(--jp-accent-hover); }
html[data-bs-theme="light"] .demo-button { background: #22303c; color: #f4f8fb; }
html[data-bs-theme="light"] .demo-button:hover,
html[data-bs-theme="light"] .demo-button:focus-visible { background: #314253; }
.demo-frame-wrap iframe { display: block; width: 100%; height: 100%; border: 0; }

/* ── Navigation cards ───────────────────────────────────── */
.jp-nav-cards {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 1rem;
  margin: 2rem 0;
}
.jp-nav-card {
  background: var(--bs-tertiary-bg);
  border: 1px solid var(--bs-border-color);
  border-radius: 0.75rem;
  padding: 1.2rem 1rem;
  text-decoration: none;
  color: var(--bs-body-color);
  transition: border-color 0.15s, transform 0.15s;
  display: block;
}
.jp-nav-card:hover {
  border-color: var(--jp-accent);
  transform: translateY(-2px);
  text-decoration: none;
  color: var(--bs-body-color);
}
.jp-nav-card-icon { font-size: 1.5rem; margin-bottom: 0.5rem; }
.jp-nav-card-title { font-weight: 700; font-size: 0.9rem; margin-bottom: 0.3rem; }
.jp-nav-card-body { font-size: 0.82rem; color: var(--bs-secondary-color); line-height: 1.4; }

/* ── Responsive tweaks ──────────────────────────────────── */
@media (max-width: 600px) {
  .jp-hero { padding: 1.5rem 0 1.25rem; }
  .demo-card { padding: 1.1rem; }
}
</style>

<script>
(() => {
  const button   = document.getElementById('load_demo_button');
  const card     = document.getElementById('demo_card');
  const frameWrap = document.getElementById('demo_frame_wrap');
  const frame    = document.getElementById('demo_frame');
  if (!button || !card || !frameWrap || !frame) return;

  const buildFrameUrl = () => {
    const theme = document.documentElement.getAttribute('data-bs-theme') === 'light'
      ? 'light' : 'dark';
    return `https://jitterphysics.com/AppBundle/index.html?embed=1&theme=${theme}`;
  };

  button.addEventListener('click', () => {
    if (!frame.src) frame.src = buildFrameUrl();
    card.hidden = true;
    frameWrap.hidden = false;
  }, { once: true });

  new MutationObserver(() => {
    if (frame.src) frame.src = buildFrameUrl();
  }).observe(document.documentElement, {
    attributes: true,
    attributeFilter: ['data-bs-theme']
  });
})();
</script>
