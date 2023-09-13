---
sidebar_position: 1
---

# Project Setup

### Requirements

Install the [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0).

Ensure that dotnet is correctly set up by executing the following command:

```sh
dotnet --version
```

### Create a New Console Application and Add Jitter and Raylib

First, create a new directory named "BoxDrop" and navigate into it:

```sh
mkdir BoxDrop && cd BoxDrop
```

Next, create a new console application in this directory and add Raylib#:

```sh
dotnet new console
dotnet add package Raylib-cs --version 4.5.0.4
```

Now, add Jitter2 as a reference to this project. Clone the git repository and add the reference:

```sh
dotnet add reference ./../{correct_path}/Jitter2/src/Jitter2/Jitter2.csproj 
```

You have completed the setup. If you now execute the following command:

```sh
dotnet run
```

Your console should display: "Hello, World!".
