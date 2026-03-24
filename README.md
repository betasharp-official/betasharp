



<p align="center">
<img height="256" src="BetaSharp.Launcher/logo.png" alt="BetaSharp">
<h1 align="center">BetaSharp</h1>
<p align="center">An enhanced version of Minecraft Beta 1.7.3, ported to C#.</p>
</p>
<p align="center">
<a href="https://discord.gg/x9AGsjnWv4"><img src="https://img.shields.io/badge/chat%20on-discord-7289DA" alt="Discord"></a>
<img src="https://img.shields.io/badge/language-C%23-512BD4" alt="C#">
<img src="https://img.shields.io/badge/framework-.NET-512BD4" alt=".NET">
<img src="https://img.shields.io/github/issues/Fazin85/betasharp" alt="Issues">
<img src="https://img.shields.io/github/issues-pr/Fazin85/betasharp" alt="Pull requests">
</p>


# Notice

> [!IMPORTANT]
> This project is based on decompiled Minecraft Beta 1.7.3 code and requires a legally purchased copy of the game.\
> We do not support or condone piracy. Please purchase Minecraft from [minecraft.net](https://www.minecraft.net).

## Running

The launcher is the recommended way to play. It authenticates with your Microsoft account and starts the client automatically. \
Clone the repository and run the following commands.

```
cd BetaSharp.Launcher
dotnet run --configuration Release
```

## Building

Clone the repository and make sure the .NET 10 SDK is installed. For installation, visit [dotnet.microsoft.com/en-us/download](https://dotnet.microsoft.com/en-us/download). \
The Website lists instructions for downloading the SDK on Windows, macOS and Linux.

It is recommended to build with `--configuration Release` for better performance. \
The server and client expect the JAR file to be in their running directory.

```
cd BetaSharp.(Launcher/Client/Server)
dotnet build
```

## Contributing

Contributions are welcome! Please read our [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests.

This is a personal project with no guarantees on review or merge timelines. Feel free to submit contributions, though they may or may not be reviewed or merged depending on the maintainer's availability and discretion.
