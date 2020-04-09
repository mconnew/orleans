using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Runtime;
using Orleans.Services;

namespace Orleans
{
    public interface IReminderService : IGrainService
    {
        Task Start();
        Task Stop();

        /// <summary>
        /// Registers a new reminder or updates an existing one
        /// </summary>
        /// <param name="grainId"></param>
        /// <param name="reminderName"></param>
        /// <param name="dueTime"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        Task<IGrainReminder> RegisterOrUpdateReminder(GrainId grainId, string reminderName, TimeSpan dueTime, TimeSpan period);

        Task UnregisterReminder(IGrainReminder reminder);

        Task<IGrainReminder> GetReminder(GrainId grainId, string reminderName);

        Task<List<IGrainReminder>> GetReminders(GrainId grainId);
    }
}
