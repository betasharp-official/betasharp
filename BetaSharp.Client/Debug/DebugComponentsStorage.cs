using BetaSharp.Client.Debug.Components;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Debug;

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
        void Right(DebugComponent comp)
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

        Right(new DebugFramework());
        Right(new DebugMemory());
        Right(new DebugSeparator());
        Right(new DebugSystem());
        Right(new DebugSeparator());
        Right(new DebugTargetedBlock());
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

                    DebugComponent? comp = DebugComponents.CreateFromTypeName(parts[0]);
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
