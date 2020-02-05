using System;
using System.Threading.Tasks;
using Orleans.CodeGeneration;
using Orleans.Core;
using Orleans.Metadata.NewGrainRefSystem;
using Orleans.Runtime;

namespace Orleans
{
    /// <summary>
    /// Extension methods for grains.
    /// </summary>
    public static class GrainExtensions
    {
        private const string WRONG_GRAIN_ERROR_MSG = "Passing a half baked grain as an argument. It is possible that you instantiated a grain class explicitly, as a regular object and not via Orleans runtime or via proper test mocking";

        internal static IGrainReference AsWeaklyTypedReference(this IAddressable grain) => grain switch
        {
            GrainReference reference => reference,
            null => throw new ArgumentNullException(nameof(grain)),
            _ => new UntypedGrainReference(grain.GetGrainId())
        };

        /// <summary>
        /// Converts this grain to a specific grain interface.
        /// </summary>
        /// <typeparam name="TGrainInterface">The type of the grain interface.</typeparam>
        /// <param name="grain">The grain to convert.</param>
        /// <returns>A strongly typed <c>GrainReference</c> of grain interface type TGrainInterface.</returns>
        public static TGrainInterface AsReference<TGrainInterface>(this IAddressable grain)
        {
            ThrowIfNullGrain(grain);
            var grainReference = grain.AsWeaklyTypedReference();
            return grainReference.Runtime.Convert<TGrainInterface>(grainReference);
        }

        /// <summary>
        /// Casts a grain to a specific grain interface.
        /// </summary>
        /// <typeparam name="TGrainInterface">The type of the grain interface.</typeparam>
        /// <param name="grain">The grain to cast.</param>
        public static TGrainInterface Cast<TGrainInterface>(this IAddressable grain)
        {
            return grain.AsReference<TGrainInterface>();
        }

        /// <summary>
        /// Casts the provided <paramref name="grain"/> to the provided <paramref name="interfaceType"/>.
        /// </summary>
        /// <param name="grain">The grain.</param>
        /// <param name="interfaceType">The resulting interface type.</param>
        /// <returns>A reference to <paramref name="grain"/> which implements <paramref name="interfaceType"/>.</returns>
        public static object Cast(this IAddressable grain, Type interfaceType)
        {
            return grain.AsWeaklyTypedReference().Runtime.Convert(grain, interfaceType);
        }

        /// <summary>
        /// Binds the grain reference to the provided <see cref="IGrainFactory"/>.
        /// </summary>
        /// <param name="grain">The grain reference.</param>
        /// <param name="grainFactory">The grain factory.</param>
        public static void BindGrainReference(this IAddressable grain, IGrainFactory grainFactory)
        {
            grainFactory.BindGrainReference(grain);
        }

        public static GrainId GetGrainId(this IAddressable grain)
        {
            switch (grain)
            {
                case Grain grainBase:
                    if (grainBase.GrainId.IsDefault)
                    {
                        throw new ArgumentException(WRONG_GRAIN_ERROR_MSG, "grain");
                    }
                    return grainBase.GrainId;
                case GrainReference grainReference:
                    if (grainReference.GrainId.IsDefault)
                    {
                        throw new ArgumentException(WRONG_GRAIN_ERROR_MSG, "grain");
                    }
                    return grainReference.GrainId;
                case ISystemTargetBase systemTarget:
                    return systemTarget.GrainId;
                default:
                    throw new ArgumentException($"{nameof(GetGrainId)} has been called on an unexpected type: {grain.GetType().FullName}.", "grain");
            }
        }

        /// <summary>
        /// Returns whether part of the primary key is of type long.
        /// </summary>
        /// <param name="grain">The target grain.</param>
        public static bool IsPrimaryKeyBasedOnLong(this IAddressable grain)
        {
            return ((LegacyGrainId)GetGrainId(grain)).IsLongKey;
        }

        /// <summary>
        /// Returns the long representation of a grain primary key.
        /// </summary>
        /// <param name="grain">The grain to find the primary key for.</param>
        /// <param name="keyExt">The output parameter to return the extended key part of the grain primary key, if extended primary key was provided for that grain.</param>
        /// <returns>A long representing the primary key for this grain.</returns>
        public static long GetPrimaryKeyLong(this IAddressable grain, out string keyExt)
        {
            return ((LegacyGrainId)GetGrainId(grain)).GetPrimaryKeyLong(out keyExt);
        }

        /// <summary>
        /// Returns the long representation of a grain primary key.
        /// </summary>
        /// <param name="grain">The grain to find the primary key for.</param>
        /// <returns>A long representing the primary key for this grain.</returns>
        public static long GetPrimaryKeyLong(this IAddressable grain)
        {
            return ((LegacyGrainId)GetGrainId(grain)).GetPrimaryKeyLong();
        }

        /// <summary>
        /// Returns the Guid representation of a grain primary key.
        /// </summary>
        /// <param name="grain">The grain to find the primary key for.</param>
        /// <param name="keyExt">The output parameter to return the extended key part of the grain primary key, if extended primary key was provided for that grain.</param>
        /// <returns>A Guid representing the primary key for this grain.</returns>
        public static Guid GetPrimaryKey(this IAddressable grain, out string keyExt)
        {
            return ((LegacyGrainId)GetGrainId(grain)).GetPrimaryKey(out keyExt);
        }

        /// <summary>
        /// Returns the Guid representation of a grain primary key.
        /// </summary>
        /// <param name="grain">The grain to find the primary key for.</param>
        /// <returns>A Guid representing the primary key for this grain.</returns>
        public static Guid GetPrimaryKey(this IAddressable grain)
        {
            return ((LegacyGrainId)GetGrainId(grain)).GetPrimaryKey();
        }

        /// <summary>
        /// Returns the string primary key of the grain.
        /// </summary>
        /// <param name="grain">The grain to find the primary key for.</param>
        /// <returns>A string representing the primary key for this grain.</returns>
        public static string GetPrimaryKeyString(this IAddressable grain)
        {
            return ((LegacyGrainId)GetGrainId(grain)).GetPrimaryKeyString();
        }

        public static long GetPrimaryKeyLong(this IGrain grain, out string keyExt)
        {
            return ((LegacyGrainId)GetGrainId(grain)).GetPrimaryKeyLong(out keyExt);
        }
        public static long GetPrimaryKeyLong(this IGrain grain)
        {
            return ((LegacyGrainId)GetGrainId(grain)).PrimaryKeyLong;
        }
        public static Guid GetPrimaryKey(this IGrain grain, out string keyExt)
        {
            return ((LegacyGrainId)GetGrainId(grain)).GetPrimaryKey(out keyExt);
        }
        public static Guid GetPrimaryKey(this IGrain grain)
        {
            return ((LegacyGrainId)GetGrainId(grain)).PrimaryKey;
        }

        public static string GetPrimaryKeyString(this IGrainWithStringKey grain)
        {
            return ((LegacyGrainId)GetGrainId(grain)).PrimaryKeyString;
        }

        private static void ThrowIfNullGrain(IAddressable grain)
        {
            if (grain == null)
            {
                throw new ArgumentNullException(nameof(grain));
            }
        }
    }
}
