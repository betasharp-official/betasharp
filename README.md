# BetaSharp

[![Discord](https://img.shields.io/badge/chat%20on-discord-7289DA)](https://discord.gg/x9AGsjnWv4)
![C#](https://img.shields.io/badge/language-C%23-512BD4)
![.NET](https://img.shields.io/badge/framework-.NET-512BD4)
![Issues](https://img.shields.io/github/issues/Fazin85/betasharp)
![Pull requests](https://img.shields.io/github/issues-pr/Fazin85/betasharp)

An enhanced version of Minecraft Beta 1.7.3, ported to C#.

# Notice

> [!IMPORTANT]
> This project is based on decompiled Minecraft Beta 1.7.3 code and requires a legally purchased copy of the game.\
> We do not support or condone piracy. Please purchase Minecraft from https://www.minecraft.net.

## Running

The launcher is the recommended way to play. It authenticates with your Microsoft account and starts the client automatically. \
Clone the repository and run the following commands.

```
cd BetaSharp.Launcher
dotnet run --configuration Release
```

## Building

Clone the repository and make sure the .NET 10 SDK is installed. For installation, visit https://dotnet.microsoft.com/en-us/download. \
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

# Verification Walkthrough: Linux Renderer Bug Fix (Issue #141)

## What was Changed
The rendering offset bug on Linux window managers (such as Wayland or Niri) was fixed by differentiating between the **logical window size** and the **physical framebuffer size**.

- **Added Framebuffer awareness:** Expanded `Display.cs` with `getFramebufferWidth()` and `getFramebufferHeight()` which fetch the pixel resolution directly from Silk.NET's `IWindow.FramebufferSize`.
- **Viewport Fix:** Upgraded `GLManager.GL.Viewport()` calls in `BetaSharp.cs` and `GameRenderer.cs` to utilize the Framebuffer dimensions instead of the logical window boundaries.
- **FBO Sizing Fix:** Updated `PostProcessManager` initialization and resizing to create Framebuffer Objects (FBOs) matching the physical Framebuffer size. This fixes an issue where drawing a full-resolution Native Viewport inside a smaller Logical-Size FBO caused the image to appear cropped.
- **Screenshot Fix:** Updated screenshot functionality (`ReadPixels`) to read exact pixel quantities scaled to the Framebuffer, ensuring captured screenshots mirror exactly what is rendered without boundary cutoff.

## 1.1 Supplemental Fixes (Fullscreen & macOS)
- **Fullscreen Cursor / Stretch Alignment Fix:** An issue where pressing `F11` would cause the window projection to assume the fullscreen desktop unscaled resolution (stretching the picture and throwing off the GUI cursor scaling coordinates) was caught. Fix was applied in `Display.cs` and `toggleFullscreen` inside `BetaSharp.cs` to correctly retain the window's true logical bounds during viewport resizing.
- **macOS (M4) Performance Regression Fix:** Previously, updating the Framebuffer and Window Size inside `BetaSharp` forced an internal call to the OS window manager. On macOS/Apple Silicon, this synchronously halted the game's thread on Cocoa's UI thread, destroying performance. Refactored `Display.cs` to cache Window and Framebuffer sizes on `Resize` events, bypassing the per-frame window manager polling entirely.
- **macOS Sound Effects Mute Fix V1 (Spatialization):** macOS OpenAL instances are known to entirely drop Stereo audio buffers if `RelativeToListener = false` and `Position` properties (spatialization functions) are called on them. Updated `SoundManager.cs` to detect multi-channel sound effect loading, and bypass 3D spatialization for them forcing `RelativeToListener = true`, allowing macOS to successfully play SFML.Audio Sound FX.
- **macOS Sound Effects Mute Fix V2 (Channel Limit Exhaustion):** macOS native CoreAudio/OpenAL implementation limits globally available active hardware sound sources to 16. `BetaSharp` was attempting to instantiate `MaxChannels = 32`, exhausting OpenAL's capacity and causing all successive UI sound clicks and block placements to silently drop. Imposed a restrictive cap of `MaxChannels = 14` leaving enough global headroom for background streaming `.ogg` tracks.

## Validation Status
- **Compilation Check (`dotnet build`)**: Passed successfully with 0 errors.

## Next Steps for User
Since these issues are specific to OS and Window Configurations, please run the game and verify the following:
1. Start the game in windowed mode. Ensure the black top bar has disappeared.
2. Press `F11` to enter Fullscreen Mode. Verify that the image stretches properly, and the Mouse pointer coordinates correctly correspond to the interface buttons.
3. Try taking an in-game screenshot (`F2`) to confirm images are captured correctly and match your screen dimensions.
4. **macOS / M4 Players:** Verify that your framerate and extreme lag spikes are completely gone.
5. **macOS Players:** Verify that Sound Effects (UI Clicks, block destroying sounds) now play properly alongside the music track.
