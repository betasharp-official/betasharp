using BetaSharp.Stats;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Threading;

internal class ThreadStatSynchronizerSend
{
    private readonly ILogger<ThreadStatSynchronizerSend> _logger = Log.Instance.For<ThreadStatSynchronizerSend>();
    private readonly StatsSynchronizer _synchronizer;
    private readonly Dictionary<StatBase, int> _statsMap;

    public ThreadStatSynchronizerSend(StatsSynchronizer synchronizer, Dictionary<StatBase, int> statsMap)
    {
        _synchronizer = synchronizer;
        _statsMap = statsMap;
    }

    public void Start()
    {
        Task.Run(() =>
        {
            try
            {
                _synchronizer.SaveStatsToFile(_statsMap, _synchronizer.UnsentStatsFile, _synchronizer.TempUnsentStatsFile, _synchronizer.OldUnsentStatsFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Stat Synchronizer Send");
            }
            finally
            {
                _synchronizer.Busy = false;
            }
        });
    }
}
