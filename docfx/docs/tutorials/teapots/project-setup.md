# Project Setup

In the previous section, we created a simulation of falling boxes. Jitter includes several default shapes, such as capsules, cylinders, and spheres.
These shapes can be transformed and/or combined, and they are already sufficient to represent many types of collidable entities.

In this section, we will add a custom convex shape to the simulation—specifically, the famous *Utah teapot*. We'll construct this shape from its visual representation by loading a `teapot.obj` file and using its vertices to create the convex shape.


### Requirements

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

Ensure that dotnet is correctly set up by executing the following command:

```sh
dotnet --version
```

### Create a New Console Application and Add Jitter and Raylib

First, create a new directory named "TeaDrop" and navigate into it:

```sh
mkdir TeaDrop && cd TeaDrop
```

Download and unzip the [teapot.obj](ahttps://github.com/notgiven688/jitterphysics2/raw/refs/heads/main/src/JitterDemo/assets/teapot.obj.zip) model.

```sh
wget https://github.com/notgiven688/jitterphysics2/raw/refs/heads/main/src/JitterDemo/assets/teapot.obj.zip
unzip teapot.obj.zip
```

Next, create a new console application in this directory and add Raylib-cs and Jitter2:

```sh
dotnet new console
dotnet add package Raylib-cs --version 6.1.1
dotnet add package Jitter2
```

Add the following code to `TeaDrop.csproj` to allow unsafe code, and to copy teapot.obj automatically to the output directory:

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

You have completed the setup. If you now execute the following command:

```sh
dotnet run
```

Your console should display: "Hello, World!".
