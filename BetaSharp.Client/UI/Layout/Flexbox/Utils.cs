namespace BetaSharp.Client.UI.Layout.Flexbox;

// MeasureFunc describes function for measuring
public delegate Size MeasureFunc(Node node, float width, MeasureMode widthMode, float height, MeasureMode heightMode);

// PrintFunc defines function for printing
public delegate void PrintFunc(Node node);

// BaselineFunc describes function for baseline
public delegate float BaselineFunc(Node node, float width, float height);

// Logger defines logging function
public delegate int LoggerFunc(Config config, Node node, LogLevel level, string format, params object[] args);
