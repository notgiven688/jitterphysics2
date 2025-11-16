---
sidebar_position: 1
---

# Project Setup

In this project we will use Raylib and Jitter to implement a simple scene of boxes falling to the ground.

### Requirements

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

Ensure that dotnet is correctly set up by executing the following command:

```sh
dotnet --version
```

### Create a New Console Application and Add Jitter and Raylib

First, create a new directory named "BoxDrop" and navigate into it:

```sh
mkdir BoxDrop && cd BoxDrop
```

Next, create a new console application in this directory and add Raylib-cs and Jitter2:

```sh
dotnet new console
dotnet add package Raylib-cs --version 6.1.1
dotnet add package Jitter2
```

You have completed the setup. If you now execute the following command:

```sh
dotnet run
```

Your console should display: "Hello, World!".
