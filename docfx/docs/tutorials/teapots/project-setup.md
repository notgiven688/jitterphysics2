# Project Setup

The Boxes tutorial used built-in shapes. This tutorial uses vertices from a model to create a collision shape for the *Utah teapot*. We will load `teapot.obj`, simplify its vertices, and stack several teapots in the simulation.


### Requirements

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

Check that the SDK is installed:

```sh
dotnet --version
```

### Create a New Console Application and Add Jitter and Raylib

First, create a new directory named "TeaDrop" and navigate into it:

```sh
mkdir TeaDrop && cd TeaDrop
```

Download and unzip the [teapot.obj](https://github.com/notgiven688/jitterphysics2/raw/refs/heads/main/src/JitterDemo/assets/teapot.obj.zip) model.

```sh
wget https://github.com/notgiven688/jitterphysics2/raw/refs/heads/main/src/JitterDemo/assets/teapot.obj.zip
unzip teapot.obj.zip
```

Create a console application and add Raylib-cs and Jitter2:

```sh
dotnet new console
dotnet add package Raylib-cs --version 6.1.1
dotnet add package Jitter2
```

Add these settings inside the `<Project>` element in `TeaDrop.csproj`. They allow access to Raylib's mesh data through pointers and copy `teapot.obj` to the output directory:

```xml
  <PropertyGroup>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <None Update="teapot.obj">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

Run the application:

```sh
dotnet run
```

The console should print `Hello, World!`.
