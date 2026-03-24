<p align="center">
<img height="128" src="BetaSharp.Launcher/logo.png" alt="BetaSharp">
<h1 align="center">BetaSharp</h1>
<p align="center">An enhanced version of Minecraft Beta 1.7.3, written in C#.</p>
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
> BetaSharp requires a legally purchased copy of Minecraft. We do not support or condone piracy. Please purchase Minecraft at [minecraft.net](https://www.minecraft.net).

## Running

The launcher is the recommended way to play, it authenticates with your Microsoft account and starts the client automatically. \
Clone the repository and run the following commands.

```
cd BetaSharp.Launcher
dotnet run --configuration Release
```

## Building

Clone the repository and make sure the .NET 10 SDK is installed. For installation, visit [dotnet.microsoft.com](https://dotnet.microsoft.com/en-us/download). \
The Website lists instructions for downloading the SDK on Windows, macOS and Linux.

It is recommended to build with `--configuration Release` for better performance. \
The server and client expect the JAR file to be in their running directory.

```
cd BetaSharp.(Launcher/Client/Server)
dotnet build
```

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for the code of conduct and pull request process. \
This is a personal project, so review and merge timelines aren't guaranteed, but submissions are appreciated.
