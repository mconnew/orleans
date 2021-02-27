using Orleans;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.ServiceBus.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnitTests.GrainInterfaces;
using UnitTests.Grains;

namespace ServiceBus.Tests.MonitorTests
{
    public class CacheMonitorForTesting : ICacheMonitor
    {
        public CacheMonitorCounters CallCounters { get; } = new CacheMonitorCounters();
        
        public void TrackCachePressureMonitorStatusChange(string pressureMonitorType, bool underPressure, double? cachePressureContributionCount, double? currentPressure,
            double? flowControlThreshold)
        {
            Interlocked.Increment(ref CallCounters.TrackCachePressureMonitorStatusChangeCallCounter);
        }

        public void ReportCacheSize(long totalCacheSizeInByte)
        {
            Interlocked.Increment(ref CallCounters.ReportCacheSizeCallCounter);
        }

        public void ReportMessageStatistics(DateTime? oldestMessageEnqueueTimeUtc, DateTime? oldestMessageDequeueTimeUtc, DateTime? newestMessageEnqueueTimeUtc, long totalMessageCount)
        {
            Interlocked.Increment(ref CallCounters.ReportMessageStatisticsCallCounter);
        }

        public void TrackMemoryAllocated(int memoryInByte)
        {
            Interlocked.Increment(ref CallCounters.TrackMemoryAllocatedCallCounter);
        }

        public void TrackMemoryReleased(int memoryInByte)
        {
            Interlocked.Increment(ref CallCounters.TrackMemoryReleasedCallCounter);
        }

        public void TrackMessagesAdded(long mesageAdded)
        {
            Interlocked.Increment(ref CallCounters.TrackMessageAddedCounter);
        }

        public void TrackMessagesPurged(long messagePurged)
        {
            Interlocked.Increment(ref CallCounters.TrackMessagePurgedCounter);
        }
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    public class CacheMonitorCounters
    {
        [Hagar.Id(0)]
        public int TrackCachePressureMonitorStatusChangeCallCounter;
        [Hagar.Id(1)]
        public int ReportCacheSizeCallCounter;
        [Hagar.Id(2)]
        public int ReportMessageStatisticsCallCounter;
        [Hagar.Id(3)]
        public int TrackMemoryAllocatedCallCounter;
        [Hagar.Id(4)]
        public int TrackMemoryReleasedCallCounter;
        [Hagar.Id(5)]
        public int TrackMessageAddedCounter;
        [Hagar.Id(6)]
        public int TrackMessagePurgedCounter;
    }
}
