using BetaSharp;
using BetaSharp.Util;
using Microsoft.Extensions.Logging;

Log.Instance.Initialize(PathHelper.GetAppDir(nameof(BetaSharp)));

try
{
    BetaSharp.Client.BetaSharp.Startup(args);
}
catch (Exception exception)
{
    Log.Instance.For<Program>().LogError(exception, "Unhandled exception occured!");

#if DEBUG
    throw;
#endif
}
