[![JitterTests](https://github.com/notgiven688/jitterphysics2/actions/workflows/jitter-tests.yml/badge.svg)](https://github.com/notgiven688/jitterphysics2/actions/workflows/jitter-tests.yml)

# Jitter Physics 2

Successor of [Jitter Physics](https://github.com/notgiven688/jitterphysics). It is an impulse-based dynamics engine with a semi-implicit Euler integrator. This is a fast, simple, and dependency-free physics engine written in C#.

<img src="./docs/docs/img/jitter_screenshot0.png" alt="screenshot" width="400"/> <img src="./docs/docs/img/jitter_screenshot1.png" alt="screenshot" width="400"/>

<img src="./docs/docs/img/jitter_screenshot2.png" alt="screenshot" width="400"/> <img src="./docs/docs/img/jitter_screenshot3.png" alt="screenshot" width="400"/>

## Getting Started

Jitter is cross-platform. The `src` directory contains four projects:

| Project         | Description                                          |
| --------------- | ---------------------------------------------------- |
| Jitter2         | The Jitter2 library.                                 |
| JitterDemo      | Demo scenes rendered with OpenGL. Tested under Linux and Windows. |
| JitterBenchmark | Setup for benchmarks utilizing BenchmarkDotNet.      |
| JitterTests     | NUnit unit tests.                                    |

To run the demo scenes:

```
- Install the [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- git clone https://github.com/notgiven688/jitter2.git
- cd ./Jitter2/src/JitterDemo && dotnet run -c Release
```

JitterDemo uses [GLFW](https://www.glfw.org/) for accessing OpenGL and managing windows, and [cimgui](https://github.com/cimgui/cimgui) for GUI rendering. The project contains these native binaries in a precompiled form.

## Features

- [x] Speculative contacts (avoiding the bullet-through-paper problem).
- [x] A variety of constraints and motors with support for softness.
- [x] A sophisticated deactivation scheme with minimal cost for inactive rigid bodies (scenes with 100k inactive bodies are easily possible).
- [x] Edge collision filter for internal edges of triangle meshes.
- [x] Substepping for improved constraint and contact stability.
- [x] Generic convex-convex collision detection using EPA-aided MPR.
- [x] "One-shot" contact manifolds using auxiliary contacts for flat surface collisions.
- [x] Efficient compound shapes.
- [x] Easy integration of custom shapes. Integrated: Box, Capsule, Cone, Convex Hull, Point Cloud, Sphere, Triangle, Transformed.

## Documentation

Find the [documentation here](https://notgiven688.github.io/jitterphysics2).

## Contribute 👋

Every contribution is welcome! Fork the project and create a pull request.
