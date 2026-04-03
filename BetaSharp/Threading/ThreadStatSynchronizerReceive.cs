using BetaSharp.Stats;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Threading;

internal class ThreadStatSynchronizerReceive
{
    private readonly ILogger<ThreadStatSynchronizerReceive> _logger = Log.Instance.For<ThreadStatSynchronizerReceive>();
    private readonly StatsSynchronizer _synchronizer;

    public ThreadStatSynchronizerReceive(StatsSynchronizer synchronizer)
    {
        _synchronizer = synchronizer;
    }

    public void Start()
    {
        Task.Run(() =>
        {
            try
            {
                if (_synchronizer.MergedData != null)
                {
                    _synchronizer.SaveStatsToFile(_synchronizer.MergedData, _synchronizer.StatsFile, _synchronizer.TempStatsFile, _synchronizer.OldStatsFile);
                }
                else if (File.Exists(_synchronizer.StatsFile))
                {
                    _synchronizer.MergedData = _synchronizer.FetchNewestAvailableStats(_synchronizer.StatsFile, _synchronizer.TempStatsFile, _synchronizer.OldStatsFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Stat Synchronizer Receive");
            }
            finally
            {
                _synchronizer.Busy = false;
            }
        });
    }
}
