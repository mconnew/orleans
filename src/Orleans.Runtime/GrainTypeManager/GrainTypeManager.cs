using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.ApplicationParts;
using Orleans.CodeGeneration;
using Orleans.Configuration;
using Orleans.GrainDirectory;
using Orleans.Metadata;
using Orleans.Serialization;
using Orleans.Utilities;

namespace Orleans.Runtime
{
    namespace NewNewNew
    {
        public readonly struct PlacementConstraintContext
        {
            public PlacementConstraintContext(
                SiloAddress silo,
                GrainType grainType,
                GrainInterfaceMetadata localProperties,
                GrainInterfaceMetadata remoteProperties)
            {
                this.Silo = silo;
                this.GrainType = grainType;
                this.LocalInterfaceProperties = localProperties;
                this.RemoteInterfaceProperties = remoteProperties;
            }

            public readonly SiloAddress Silo;
            public readonly GrainType GrainType;
            public readonly GrainInterfaceMetadata LocalInterfaceProperties;
            public readonly GrainInterfaceMetadata RemoteInterfaceProperties;
        }

        // Alternate names:
        // - IActivationConstraint
        // - IRoutingConstraint
        //
        // Some constraints will consider only the grainType & silo (eg, heterogeneous clusters), others will consider only the local & remote metadata (eg versioning)
        public interface IPlacementConstraint
        {
            bool IsCompatible(in PlacementConstraintContext context);
        }

        public class PlacementConstraintAggregator
        {
            private readonly IPlacementConstraint[] constraints;

            public PlacementConstraintAggregator(IEnumerable<IPlacementConstraint> constraints)
            {
                this.constraints = constraints.ToArray();
            }

            public bool IsCompatible(in PlacementConstraintContext context)
            {
                foreach (var constraint in this.constraints)
                {
                    if (!constraint.IsCompatible(context))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public class GrainRoutingMap
        {
            //public List<SiloAddress> GetCompatibleSilos(GrainType grainType, GrainInterfaceProperties interfaceMetadata);
        }
    }

    internal class GrainTypeManager
    {
        private Dictionary<SiloAddress, GrainInterfaceMap> grainInterfaceMapsBySilo;
        private Dictionary<int, List<SiloAddress>> supportedSilosByTypeCode;
        private readonly ILogger logger;
        private readonly GrainInterfaceMap grainInterfaceMap;
        private readonly Dictionary<string, GrainTypeData> grainTypes;
        private readonly Dictionary<int, InvokerData> invokers;
        private readonly SerializationManager serializationManager;
		private readonly PlacementStrategy defaultPlacementStrategy;
        private Dictionary<int, Dictionary<ushort, List<SiloAddress>>> supportedSilosByInterface;

        internal IReadOnlyDictionary<SiloAddress, GrainInterfaceMap> GrainInterfaceMapsBySilo => this.grainInterfaceMapsBySilo;

        public IEnumerable<KeyValuePair<string, GrainTypeData>> GrainClassTypeData => this.grainTypes;

        public GrainInterfaceMap ClusterGrainInterfaceMap { get; private set; }

        public IGrainTypeResolver GrainTypeResolver { get; private set; }

        public GrainTypeManager(
            IApplicationPartManager applicationPartManager,
            PlacementStrategy defaultPlacementStrategy,
            SerializationManager serializationManager,
            ILogger<GrainTypeManager> logger,
            IOptions<GrainClassOptions> grainClassOptions)
        {
            this.logger = logger;
            this.defaultPlacementStrategy = defaultPlacementStrategy;
            this.serializationManager = serializationManager;
            grainInterfaceMap = new GrainInterfaceMap(this.defaultPlacementStrategy);
            ClusterGrainInterfaceMap = grainInterfaceMap;
            GrainTypeResolver = grainInterfaceMap.GetGrainTypeResolver();
            grainInterfaceMapsBySilo = new Dictionary<SiloAddress, GrainInterfaceMap>();

            var grainClassFeature = applicationPartManager.CreateAndPopulateFeature<GrainClassFeature>();
            this.grainTypes = CreateGrainTypeMap(grainClassFeature, grainClassOptions.Value);

            var grainInterfaceFeature = applicationPartManager.CreateAndPopulateFeature<GrainInterfaceFeature>();
            this.invokers = CreateInvokerMap(grainInterfaceFeature);
            this.InitializeInterfaceMap();
        }

        public void Start()
        {
            LogGrainTypesFound(this.logger, this.grainTypes);
            this.serializationManager.LogRegisteredTypes();
            CrashUtils.GrainTypes = this.grainTypes.Keys.ToList();
        }

        public Dictionary<string, string> GetGrainInterfaceToClassMap()
        {
            return grainInterfaceMap.GetPrimaryImplementations();
        }

        internal bool TryGetPrimaryImplementation(string grainInterface, out string grainClass)
        {
            return grainInterfaceMap.TryGetPrimaryImplementation(grainInterface, out grainClass);
        }

        internal GrainTypeData this[string className]
        {
            get
            {
                string msg;

                lock (this)
                {
                    string grainType;

                    if (grainInterfaceMap.TryGetPrimaryImplementation(className, out grainType))
                        return grainTypes[grainType];
                    if (grainTypes.ContainsKey(className))
                        return grainTypes[className];

                    if (TypeUtils.IsGenericClass(className))
                    {
                        var templateName = TypeUtils.GetRawClassName(className);
                        if (grainInterfaceMap.TryGetPrimaryImplementation(templateName, out grainType))
                            templateName = grainType;

                        if (grainTypes.TryGetValue(templateName, out var genericGrainTypeData))
                        {
                            // Found the generic template class
                            try
                            {
                                // Instantiate the specific type from generic template
                                Type[] typeArgs = TypeUtils.GenericTypeArgsFromClassName(className);
                                var concreteTypeData = genericGrainTypeData.MakeGenericType(typeArgs);

                                // Add to lookup tables for next time
                                var grainClassName = concreteTypeData.GrainClass;
                                grainTypes.Add(grainClassName, concreteTypeData);

                                return concreteTypeData;
                            }
                            catch (Exception ex)
                            {
                                msg = "Cannot instantiate generic class " + className;
                                logger.Error(ErrorCode.Runtime_Error_100092, msg, ex);
                                throw new KeyNotFoundException(msg, ex);
                            }
                        }
                    }
                }

                msg = "Cannot find GrainTypeData for class " + className;
                logger.Error(ErrorCode.Runtime_Error_100093, msg);
                throw new TypeLoadException(msg);
            }
        }

        internal void GetTypeInfo(int typeCode, out string grainClass, out PlacementStrategy placement, string genericArguments = null)
        {
            if (!ClusterGrainInterfaceMap.TryGetTypeInfo(typeCode, out grainClass, out placement, genericArguments))
                throw new OrleansException(string.Format("Unexpected: Cannot find an implementation class for grain interface {0}", typeCode));
        }

        internal void SetInterfaceMapsBySilo(Dictionary<SiloAddress, GrainInterfaceMap> value)
        {
            grainInterfaceMapsBySilo = value;
            RebuildFullGrainInterfaceMap();
        }

        internal IReadOnlyList<SiloAddress> GetSupportedSilos(int typeCode)
        {
            return supportedSilosByTypeCode[typeCode];
        }

        internal IReadOnlyDictionary<ushort, IReadOnlyList<SiloAddress>> GetSupportedSilos(int typeCode, int ifaceId, IReadOnlyList<ushort> versions)
        {
            var result = new Dictionary<ushort, IReadOnlyList<SiloAddress>>();
            foreach (var version in versions)
            {
                var silosWithTypeCode = supportedSilosByTypeCode[typeCode];
                // We need to sort this so the list of silos returned will
                // be the same accross all silos in the cluster
                var silosWithCorrectVersion = supportedSilosByInterface[ifaceId][version]
                    .Intersect(silosWithTypeCode)
                    .OrderBy(addr => addr)
                    .ToList();
                result[version] = silosWithCorrectVersion;
            }
            
            return result;
        }

        internal IReadOnlyList<ushort> GetAvailableVersions(int ifaceId)
        {
            return supportedSilosByInterface[ifaceId].Keys.ToList();
        }

        internal ushort GetLocalSupportedVersion(int ifaceId)
        {
            return grainInterfaceMap.GetInterfaceVersion(ifaceId);
        }

        private static Dictionary<int, InvokerData> CreateInvokerMap(GrainInterfaceFeature grainInterfaceFeature)
        {
            var result = new Dictionary<int, InvokerData>();

            foreach (var descriptor in grainInterfaceFeature.Interfaces)
            {
                int ifaceId = descriptor.InterfaceId;

                if (result.ContainsKey(ifaceId))
                    throw new InvalidOperationException($"Grain method invoker classes {result[ifaceId]} and {descriptor.InvokerType.FullName} use the same interface id {ifaceId}");

                result[ifaceId] = new InvokerData(descriptor.InvokerType);
            }

            return result;
        }

        private void InitializeInterfaceMap()
        {
            foreach (GrainTypeData grainType in grainTypes.Values)
            {
                AddToGrainInterfaceToClassMap(grainType.Type, grainType.RemoteInterfaceTypes);
            }

            void AddToGrainInterfaceToClassMap(Type grainClass, IEnumerable<Type> grainInterfaces)
            {
                var placement = GrainTypeData.GetPlacementStrategy(grainClass, this.defaultPlacementStrategy);

                foreach (var iface in grainInterfaces)
                {
                    var isPrimaryImplementor = IsPrimaryImplementor(grainClass, iface);
                grainInterfaceMap.AddEntry(iface, grainClass, placement, isPrimaryImplementor);
                }
            }
        }

        private static bool IsPrimaryImplementor(Type grainClass, Type iface)
        {
            // If the class name exactly matches the interface name, it is considered the primary (default)
            // implementation of the interface, e.g. IFooGrain -> FooGrain
            return (iface.Name.Substring(1) == grainClass.Name);
        }

        public bool TryGetData(string name, out GrainTypeData result)
        {
            return grainTypes.TryGetValue(name, out result);
        }

        internal GrainInterfaceMap GetTypeCodeMap()
        {
            // the map is immutable at this point
            return grainInterfaceMap;
        }

        /// <summary>
        /// Returns a list of all graintypes in the system.
        /// </summary>
        /// <returns></returns>
        internal string[] GetGrainTypeList()
        {
            return grainTypes.Keys.ToArray();
        }

        internal IGrainMethodInvoker GetInvoker(int interfaceId)
        {
            try
            {
                InvokerData invokerData;
                if (invokers.TryGetValue(interfaceId, out invokerData))
                    return invokerData.GetInvoker();
            }
            catch (Exception ex)
            {
                throw new OrleansException(String.Format("Error finding invoker for interface ID: {0} (0x{0, 8:X8}). {1}", interfaceId, ex), ex);
            }

            Type type;
            var interfaceName = grainInterfaceMap.TryGetServiceInterface(interfaceId, out type) ?
                type.FullName : "*unavailable*";

            throw new OrleansException(String.Format("Cannot find an invoker for interface {0} (ID={1},0x{1, 8:X8}).",
                interfaceName, interfaceId));
        }

        private void RebuildFullGrainInterfaceMap()
        {
            var newClusterGrainInterfaceMap = new GrainInterfaceMap(this.defaultPlacementStrategy);
            var newSupportedSilosByTypeCode = new Dictionary<int, List<SiloAddress>>();
            var newSupportedSilosByInterface = new Dictionary<int, Dictionary<ushort, List<SiloAddress>>>();
            foreach (var kvp in grainInterfaceMapsBySilo)
            {
                newClusterGrainInterfaceMap.AddMap(kvp.Value);

                foreach (var supportedInterface in kvp.Value.SupportedInterfaces)
                {
                    var ifaceId = supportedInterface.InterfaceId;
                    var version = supportedInterface.InterfaceVersion;

                    var supportedSilosByVersion = newSupportedSilosByInterface.GetValueOrAddNew(ifaceId);
                    var supportedSilosForVersion = supportedSilosByVersion.GetValueOrAddNew(version);
                    supportedSilosForVersion.Add(kvp.Key);
                }

                foreach (var grainClassData in kvp.Value.SupportedGrainClassData)
                {
                    var grainType = grainClassData.GrainTypeCode;

                    var supportedSilos = newSupportedSilosByTypeCode.GetValueOrAddNew(grainType);
                    supportedSilos.Add(kvp.Key);
                }
            }
            foreach (var silos in newSupportedSilosByTypeCode.Values)
            {
                // We need to sort this so the list of silos returned will
                // be the same accross all silos in the cluster
                silos.Sort(); 
            }
            ClusterGrainInterfaceMap = newClusterGrainInterfaceMap;
            GrainTypeResolver = ClusterGrainInterfaceMap.GetGrainTypeResolver();
            supportedSilosByTypeCode = newSupportedSilosByTypeCode;
            supportedSilosByInterface = newSupportedSilosByInterface;
        }

        private static Dictionary<string, GrainTypeData> CreateGrainTypeMap(GrainClassFeature grainClassFeature, GrainClassOptions grainClassOptions)
        {
            var result = new Dictionary<string, GrainTypeData>();

            var excluded = grainClassOptions.ExcludedGrainTypes;
            foreach (var grainClassMetadata in grainClassFeature.Classes)
            {
                var grainType = grainClassMetadata.ClassType;
                var className = TypeUtils.GetFullName(grainType);

                if (excluded != null && excluded.Contains(className)) continue;

                var typeData = new GrainTypeData(grainType);
                result[className] = typeData;
            }

            return result;
        }

        internal static void LogGrainTypesFound(ILogger logger, IDictionary<string, GrainTypeData> grainTypeData)
        {
            var sb = new StringBuilder();
            sb.AppendLine(String.Format("Loaded grain type summary for {0} types: ", grainTypeData.Count));

            foreach (var grainType in grainTypeData.Values.OrderBy(gtd => gtd.Type.FullName))
            {
                // Skip system targets and Orleans grains
                var assemblyName = grainType.Type.Assembly.FullName.Split(',')[0];
                if (!typeof(ISystemTarget).IsAssignableFrom(grainType.Type))
                {
                    int grainClassTypeCode = CodeGeneration.GrainInterfaceUtils.GetGrainClassTypeCode(grainType.Type);
                    sb.AppendFormat("Grain class {0}.{1} [{2} (0x{3})] from {4}.dll implementing interfaces: ",
                        grainType.Type.Namespace,
                        TypeUtils.GetTemplatedName(grainType.Type),
                        grainClassTypeCode,
                        grainClassTypeCode.ToString("X"),
                        assemblyName);
                    var first = true;

                    foreach (var iface in grainType.RemoteInterfaceTypes)
                    {
                        if (!first)
                            sb.Append(", ");

                        sb.Append(TypeUtils.GetTemplatedName(iface));

                        if (CodeGeneration.GrainInterfaceUtils.IsGrainType(iface))
                        {
                            int ifaceTypeCode = CodeGeneration.GrainInterfaceUtils.GetGrainInterfaceId(iface);
                            sb.AppendFormat(" [{0} (0x{1})]", ifaceTypeCode, ifaceTypeCode.ToString("X"));
                        }
                        first = false;
                    }
                    sb.AppendLine();
                }
            }

            var report = sb.ToString();
            logger.Info(ErrorCode.Loader_GrainTypeFullList, report);
        }

        private class InvokerData
        {
            private readonly Type baseInvokerType;
            private readonly CachedReadConcurrentDictionary<string, IGrainMethodInvoker> cachedGenericInvokers;
            private readonly bool isGeneric;
            private IGrainMethodInvoker invoker;

            public InvokerData(Type invokerType)
            {
                baseInvokerType = invokerType;
                this.isGeneric = invokerType.IsGenericType;
                if (this.isGeneric)
                {
                    cachedGenericInvokers = new CachedReadConcurrentDictionary<string, IGrainMethodInvoker>();
                }
            }

            public IGrainMethodInvoker GetInvoker(string genericGrainType = null)
            {
                // if the grain class is non-generic
                if (!this.isGeneric)
                {
                    return invoker ?? (invoker = (IGrainMethodInvoker)Activator.CreateInstance(baseInvokerType));
                }

                if (this.cachedGenericInvokers.TryGetValue(genericGrainType, out IGrainMethodInvoker result)) return result;

                var typeArgs = TypeUtils.GenericTypeArgsFromArgsString(genericGrainType);
                var concreteType = this.baseInvokerType.MakeGenericType(typeArgs);
                var inv = (IGrainMethodInvoker)Activator.CreateInstance(concreteType);
                this.cachedGenericInvokers.TryAdd(genericGrainType, inv);

                return inv;
            }

            public override string ToString()
            {
                return $"InvokerType: {this.baseInvokerType}";
            }
        }
    }
}
