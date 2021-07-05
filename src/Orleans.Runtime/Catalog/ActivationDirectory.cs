using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Orleans.Runtime
{
    internal class ActivationDirectory : IEnumerable<KeyValuePair<GrainId, IGrainContext>>
    {
        private readonly ConcurrentDictionary<GrainId, IGrainContext> activations = new();                // Activation data (app grains) only.
        private readonly ConcurrentDictionary<ActivationId, SystemTarget> systemTargets = new();                // SystemTarget only.
        private readonly ConcurrentDictionary<GrainId, List<IGrainContext>> grainToActivationsMap = new();     // Activation data (app grains) only.
        private readonly ConcurrentDictionary<string, CounterStatistic> systemTargetCounts = new();             // simple statistics systemTargetTypeName->count

        public int Count => activations.Count;

        public IEnumerable<SystemTarget> AllSystemTargets() => systemTargets.Select(i => i.Value);

        public IGrainContext FindTarget(GrainId key) => activations.TryGetValue(key, out var v) ? v : null;

        public SystemTarget FindSystemTarget(ActivationId key) => systemTargets.TryGetValue(key, out var v) ? v : null;

        private CounterStatistic FindSystemTargetCounter(string systemTargetTypeName)
        {
            if (systemTargetCounts.TryGetValue(systemTargetTypeName, out var ctr)) return ctr;

            var counterName = new StatisticName(StatisticNames.SYSTEM_TARGET_COUNTS, systemTargetTypeName);
            return systemTargetCounts.GetOrAdd(systemTargetTypeName, CounterStatistic.FindOrCreate(counterName, false));
        }

        public void RecordNewTarget(IGrainContext target)
        {
            if (!activations.TryAdd(target.GrainId, target))
            {
                return;
            }

            grainToActivationsMap.AddOrUpdate(target.GrainId,
                (_, t) => new() { t },
                (_, list, t) => { lock (list) list.Add(t); return list; }, target);
        }

        public void RecordNewSystemTarget(SystemTarget target)
        {
            var systemTarget = (ISystemTargetBase) target;
            systemTargets.TryAdd(target.ActivationId, target);
            if (!Constants.IsSingletonSystemTarget(systemTarget.GrainId.Type))
            {
                FindSystemTargetCounter(Constants.SystemTargetName(systemTarget.GrainId.Type)).Increment();
            }
        }

        public void RemoveSystemTarget(SystemTarget target)
        {
            var systemTarget = (ISystemTargetBase) target;
            systemTargets.TryRemove(target.ActivationId, out _);
            if (!Constants.IsSingletonSystemTarget(systemTarget.GrainId.Type))
            {
                FindSystemTargetCounter(Constants.SystemTargetName(systemTarget.GrainId.Type)).DecrementBy(1);
            }
        }

        public void RemoveTarget(IGrainContext target)
        {
            if (!activations.TryRemove(target.GrainId, out _))
                return;

            if (grainToActivationsMap.TryGetValue(target.GrainId, out var list))
            {
                lock (list)
                {
                    list.Remove(target);
                    if (list.Count == 0)
                    {
                        List<IGrainContext> list2; // == list
                        if (grainToActivationsMap.TryRemove(target.GrainId, out list2))
                        {
                            lock (list2)
                            {
                                if (list2.Count > 0)
                                {
                                    grainToActivationsMap.AddOrUpdate(target.GrainId,
                                        list2,
                                        (g, list3) => { lock (list3) list3.AddRange(list2); return list3; });
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns null if no activations exist for this grain ID, rather than an empty list
        /// </summary>
        public List<IGrainContext> FindTargets(GrainId key)
        {
            List<IGrainContext> tmp;
            if (grainToActivationsMap.TryGetValue(key, out tmp))
            {
                lock (tmp)
                {
                    return tmp.ToList();
                }
            }
            return null;
        }

        public IEnumerator<KeyValuePair<GrainId, IGrainContext>> GetEnumerator() => activations.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private readonly struct DirectoryEntry
        {
            private readonly object _value;

            public bool IsMultipleActivationGrain { get; }

            public IGrainContext SingleActivationValue
            {
                get
                {
                    if (IsMultipleActivationGrain)
                    {
                        ThrowNotSingleActivation();
                    }

                    return Unsafe.As<IGrainContext>(_value);
                }
            }

            public List<IGrainContext> MultipleActivationValue
            {
                get
                {
                    if (!IsMultipleActivationGrain)
                    {
                        ThrowNotMultipleActivation();
                    }

                    return Unsafe.As<List<IGrainContext>>(_value);
                }
            }

            private DirectoryEntry(object value, bool isMultipleActivationGrain)
            {
                _value = value;
                IsMultipleActivationGrain = isMultipleActivationGrain;
            }

            public static DirectoryEntry Create(IGrainContext grainContext) => new DirectoryEntry(grainContext, isMultipleActivationGrain: false);

            public static DirectoryEntry CreateMultiple() => new (new List<IGrainContext>(), isMultipleActivationGrain: true);

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowNotSingleActivation() => throw new InvalidOperationException("Attempted to access a multiple activation entry as a single activation entry");

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowNotMultipleActivation() => throw new InvalidOperationException("Attempted to access a single activation entry as a multipleActivation activation entry");
        }
    }
}
