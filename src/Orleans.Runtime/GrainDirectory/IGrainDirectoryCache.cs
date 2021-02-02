using System;
using System.Collections.Generic;
using Orleans.GrainDirectory;

namespace Orleans.Runtime.GrainDirectory
{
    interface IGrainDirectoryCache
    {
        /// <summary>
        /// Adds a new entry with the given version into the cache: key (grain) --> value
        /// The new entry will override any existing entry under the given key, 
        /// regardless of the stored version
        /// </summary>
        /// <param name="key">key to add</param>
        /// <param name="value">value to add</param>
        void AddOrUpdate(GrainId key, ActivationAddress value);

        /// <summary>
        /// Removes an entry from the cache given its key
        /// </summary>
        /// <param name="key">key to remove</param>
        /// <returns>True if the entry was in the cache and the removal was successful</returns>
        bool Remove(ActivationAddress key);
        
        /// <summary>
        /// Clear the cache, deleting all entries.
        /// </summary>
        void Clear();

        /// <summary>
        /// Looks up the cached value and version by the given key
        /// </summary>
        /// <returns>true if the given key is in the cache</returns>
        bool LookUp(GrainId key, out ActivationAddress result);

        /// <summary>
        /// Returns list of key-value-version tuples stored currently in the cache.
        /// </summary>
        List<ActivationAddress> Entries { get; }
    }
}
