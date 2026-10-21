# Project Setup

In this tutorial, you will use Raylib for rendering and Jitter for physics to make boxes fall onto the ground.

## Requirements

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

Check that the SDK is installed:

```sh
dotnet --version
```

## Create a New Console Application and Add Jitter and Raylib

First, create a new directory named "BoxDrop" and navigate into it:

```sh
mkdir BoxDrop && cd BoxDrop
```

Create a console application and add Raylib-cs and Jitter2:

```sh
dotnet new console
dotnet add package Raylib-cs --version 6.1.1
dotnet add package Jitter2
```

Run the application:

```sh
dotnet run
```

The console should print `Hello, World!`.
