using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.CodeGeneration;

namespace Orleans.Runtime
{
    // This class is used for activations that have extension invokers. It keeps a dictionary of 
    // invoker objects to use with the activation, and extend the default invoker
    // defined for the grain class.
    // Note that in all cases we never have more than one copy of an actual invoker;
    // we may have a ExtensionInvoker per activation, in the worst case.
    internal class ExtensionInvoker : IGrainExtensionMap
    {
        // Because calls to ExtensionInvoker are allways made within the activation context,
        // we rely on the single-threading guarantee of the runtime and do not protect the map with a lock.
        private Dictionary<GrainInterfaceId, (IGrainExtension, IGrainExtensionMethodInvoker)> extensionMap; // key is the extension interface ID

        /// <summary>
        /// Try to add an extension for the specific interface ID.
        /// Fail and return false if there is already an extension for that interface ID.
        /// Note that if an extension invoker handles multiple interface IDs, it can only be associated
        /// with one of those IDs when added, and so only conflicts on that one ID will be detected and prevented.
        /// </summary>
        internal bool TryAddExtension(GrainInterfaceId interfaceId, IGrainExtensionMethodInvoker invoker, IGrainExtension extension)
        {
            if (extensionMap == null)
            {
                extensionMap = new Dictionary<GrainInterfaceId, (IGrainExtension, IGrainExtensionMethodInvoker)>(1);
            }

            if (extensionMap.ContainsKey(interfaceId)) return false;

            extensionMap.Add(interfaceId, (extension, invoker));
            return true;
        }

        /// <summary>
        /// Removes all extensions for the specified interface id.
        /// Returns true if the chained invoker no longer has any extensions and may be safely retired.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns>true if the chained invoker is now empty, false otherwise</returns>
        public bool Remove(IGrainExtension extension)
        {
            GrainInterfaceId? interfaceId = null;

            foreach (var kv in extensionMap)
                if (kv.Value.Item1 == extension)
                {
                    interfaceId = kv.Key;
                    break;
                }

            if (!interfaceId.HasValue) // not found
                throw new InvalidOperationException(String.Format("Extension {0} is not installed",
                    extension.GetType().FullName));

            extensionMap.Remove(interfaceId.Value);
            return extensionMap.Count == 0;
        }

        public bool TryGetExtensionHandler(Type extensionType, out IGrainExtension result)
        {
            result = null;

            if (extensionMap == null) return false;

            foreach (var ext in extensionMap.Values)
                if (extensionType == ext.Item1.GetType())
                {
                    result = ext.Item1;
                    return true;
                }

            return false;
        }

        /// <summary>
        /// Invokes the appropriate grain or extension method for the request interface ID and method ID.
        /// First each extension invoker is tried; if no extension handles the request, then the base
        /// invoker is used to handle the request.
        /// The base invoker will throw an appropriate exception if the request is not recognized.
        /// </summary>
        public Task<object> Invoke(IAddressable grain, GrainInterfaceId interfaceId, InvokeMethodRequest request)
        {
            if (extensionMap == null || !extensionMap.TryGetValue(interfaceId, out var value))
                throw new InvalidOperationException(
                    String.Format("Extension invoker invoked with an unknown interface ID:{0}.", request.InterfaceTypeCode));

            var invoker = value.Item2;
            var extension = value.Item1;
            return invoker.Invoke(extension, request);
        }

        public bool IsExtensionInstalled(GrainInterfaceId interfaceId)
        {
            return extensionMap != null && extensionMap.ContainsKey(interfaceId);
        }

        /// <summary>
        /// Gets the extension from this instance if it is available.
        /// </summary>
        /// <param name="interfaceId">The interface id.</param>
        /// <param name="extension">The extension.</param>
        /// <returns>
        /// <see langword="true"/> if the extension is found, <see langword="false"/> otherwise.
        /// </returns>
        public bool TryGetExtension(GrainInterfaceId interfaceId, out IGrainExtension extension)
        {
            if (extensionMap != null && extensionMap.TryGetValue(interfaceId, out var value))
            {
                extension = value.Item1;
            }
            else
            {
                extension = null;
            }

            return extension != null;
        }
    }
}
