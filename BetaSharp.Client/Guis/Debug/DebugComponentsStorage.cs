using BetaSharp.Client.Guis.Debug.Components;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Guis.Debug;

public class DebugComponentsStorage
{
    private readonly ILogger<DebugComponentsStorage> _logger = Log.Instance.For<DebugComponentsStorage>();

    protected BetaSharp _game;
    private readonly string _componentsPath;

    public readonly DebugOverlay Overlay;

    public DebugComponentsStorage(BetaSharp game, string gameDataDir)
    {
        _game = game;
        _componentsPath = Path.Combine(gameDataDir, "components.txt");

        Overlay = new DebugOverlay(game);

        LoadComponents();
    }

    public static void DefaultComponents(List<DebugComponent> list)
    {
        void right(DebugComponent comp)
        {
            comp.Right = true;
            list.Add(comp);
        }

        list.Add(new DebugVersion());
        list.Add(new DebugFPS());
        list.Add(new DebugEntities());
        list.Add(new DebugParticles());
        list.Add(new DebugWorld());
        list.Add(new DebugSeparator());
        list.Add(new DebugLocation());
        list.Add(new DebugSeparator());
        list.Add(new DebugServer());

        right(new DebugFramework());
        right(new DebugMemory());
        right(new DebugSeparator());
        right(new DebugSystem());
        right(new DebugSeparator());
        right(new DebugTargetedBlock());
    }

    public void LoadComponents()
    {
        try
        {
            if (!File.Exists(_componentsPath))
            {
                _logger.LogInformation("No components file found when loading, setting defaults and saving");
                DefaultComponents(Overlay.Components);
                SaveComponents();
                return;
            }

            using StreamReader reader = new(_componentsPath);
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                try
                {
                    string[] parts = line.Split(':');
                    if (parts.Length != 2)
                    {
                        _logger.LogWarning("Line \"" + line + "\" isn't valid, must have two parts");
                        continue;
                    }

                    DebugComponent? comp = DebugComponents.CreateInstanceFromTypeName(parts[0]);
                    if (comp is null)
                    {
                        _logger.LogWarning("\"" + parts[0] + "\" is not a component type.");
                        continue;
                    }

                    comp.Right = parts[1] == "right";

                    Overlay.Components.Add(comp);
                }
                catch (Exception)
                {
                    _logger.LogError($"Skipping bad option: {line}");
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError($"Failed to load components: {exception.Message}");
        }
    }

    public void SaveComponents()
    {
        try
        {
            using var writer = new StreamWriter(_componentsPath);

            foreach (DebugComponent comp in Overlay.Components)
            {
                writer.WriteLine(comp.GetType().Name + ":" + (comp.Right ? "right" : "left"));
            }
        }
        catch (Exception exception)
        {
            _logger.LogError($"Failed to save components: {exception.Message}");
        }
    }
}
