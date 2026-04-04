# BetaSharp for Prism Launcher

This program allows running BetaSharp as a Prism Launcher instance. A .NET
runtime needs to be installed on the computer with Prism Launcher to run the
game.

## Creating an instance

1. Build `BetaSharp.PrismLauncher` and place its output in `client`.
```sh
dotnet build --ucr -o client
```

2. Create a zip file with `client/`, `instance.cfg` and `mmc-pack.json`.

```sh
# Example
zip instance.zip -r client instance.cfg mmc-pack.json
```

3. Import the created zip file into Prism Launcher

This is documented on Prism Launcher's [guide](https://prismlauncher.org/wiki/help-pages/zip-import/).

## How it works

The instance's wrapper command executes `BetaSharp.PrismLauncher` which emulates
Prism Launcher's own entrypoint to launch the client. Before launching it copies
the Minecraft jar along with other assets into the `minecraft` directory of the
instance so `BetaSharp.Client` can extract the assets from it.
