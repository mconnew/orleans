using System;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Orleans.Runtime.ReminderService
{
    [Reentrant]
    internal class GrainBasedReminderTable : Grain, IReminderTableGrain
    {
        private readonly Dictionary<GrainReference, Dictionary<string, ReminderEntry>> reminderTable = new Dictionary<GrainReference, Dictionary<string, ReminderEntry>>();
        private readonly ILogger logger;

        public GrainBasedReminderTable(ILogger<GrainBasedReminderTable> logger)
        {
            this.logger = logger;
        }

        public override Task OnActivateAsync()
        {
            logger.LogInformation("Activated");
            base.DelayDeactivation(TimeSpan.FromDays(10 * 365)); // Delay Deactivation for GrainBasedReminderTable virtually indefinitely.
            return Task.CompletedTask;
        }

        public override Task OnDeactivateAsync()
        {
            logger.LogInformation("Deactivated");
            return Task.CompletedTask;
        }

        public Task TestOnlyClearTable()
        {
            logger.LogInformation("TestOnlyClearTable");
            reminderTable.Clear();
            return Task.CompletedTask;
        }

        public Task<ReminderTableData> ReadRows(GrainReference grainRef)
        {
            Dictionary<string, ReminderEntry> reminders;
            reminderTable.TryGetValue(grainRef, out reminders);
            var result = reminders == null ? new ReminderTableData() : new ReminderTableData(reminders.Values.ToList());
            return Task.FromResult(result);
        }

        public Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            var range = RangeFactory.CreateRange(begin, end);

            var list = reminderTable.Where(e => range.InRange(e.Key)).SelectMany(e => e.Value.Values).ToList();

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(
                    "Selected {SelectCount} out of {TotalCount} reminders from memory for {Range}. Selected: {Reminders}",
                    list.Count,
                    reminderTable.Count,
                    range.ToString(),
                    Utils.EnumerableToString(list, e => e.ToString()));
            }

            var result = new ReminderTableData(list);
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Read {ReminderCount} reminders from memory: {Reminders}", result.Reminders.Count, Utils.EnumerableToString(result.Reminders));
            }

            return Task.FromResult(result);
        }

        public Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            ReminderEntry result = null;
            Dictionary<string, ReminderEntry> reminders;
            if (reminderTable.TryGetValue(grainRef, out reminders))
            {
                reminders.TryGetValue(reminderName, out result);
            }

            if (logger.IsEnabled(LogLevel.Trace))
            {
                if (result is null)
                {
                    logger.LogTrace("Reminder not found for grain {Grain} reminder {ReminderName} ", grainRef, reminderName);
                }
                else
                {
                    logger.LogTrace("Read for grain {Grain} reminder {ReminderName} row {Reminder}", grainRef, reminderName, result.ToString());
                }
            }

            return Task.FromResult(result);
        }

        public Task<string> UpsertRow(ReminderEntry entry)
        {
            entry.ETag = Guid.NewGuid().ToString();
            Dictionary<string, ReminderEntry> d;
            if (!reminderTable.ContainsKey(entry.GrainRef))
            {
                d = new Dictionary<string, ReminderEntry>();
                reminderTable.Add(entry.GrainRef, d);
            }

            d = reminderTable[entry.GrainRef];

            ReminderEntry old; // tracing purposes only
            d.TryGetValue(entry.ReminderName, out old); // tracing purposes only
                                                        // add or over-write
            d[entry.ReminderName] = entry;
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Upserted entry {Updated}, replaced {Replaced}", entry, old);
            }

            return Task.FromResult(entry.ETag);
        }

        public Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("RemoveRow Grain = {Grain}, ReminderName = {ReminderName}, eTag = {ETag}", grainRef, reminderName, eTag);
            }

            if (reminderTable.TryGetValue(grainRef, out var data)
                && data.TryGetValue(reminderName, out var e)
                && string.Equals(e.ETag, eTag, StringComparison.Ordinal))
            {
                data.Remove(reminderName);
                if (data.Count == 0)
                {
                    reminderTable.Remove(grainRef);
                }

                return Task.FromResult(true);
            }

            logger.LogWarning(
                (int)ErrorCode.RS_Table_Remove,
                "RemoveRow failed for Grain = {Grain}, ReminderName = {ReminderName}, eTag = {ETag}. Table now is: {3}",
                grainRef,
                reminderName,
                eTag,
                Utils.EnumerableToString(reminderTable.Values.SelectMany(x => x.Values)));

            return Task.FromResult(false);
        }
    }
}
