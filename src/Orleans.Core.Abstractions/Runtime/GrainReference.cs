using System;
using System.Threading.Tasks;
using Hagar;
using Hagar.Cloning;
using Hagar.Codecs;
using Hagar.Invocation;
using Hagar.Serializers;
using Microsoft.Extensions.DependencyInjection;
using Orleans.CodeGeneration;

namespace Orleans.Runtime
{
    /// <summary>
    /// Properties common to <see cref="GrainReference"/> instances with the same <see cref="Orleans.Runtime.GrainType"/> and <see cref="GrainInterfaceType"/>.
    /// </summary>
    public class GrainReferenceShared
    {
        public GrainReferenceShared(
            GrainType graintype,
            GrainInterfaceType grainInterfaceType,
            ushort interfaceVersion,
            IGrainReferenceRuntime runtime,
            InvokeMethodOptions invokeMethodOptions)
        {
            this.GrainType = graintype;
            this.InterfaceType = grainInterfaceType;
            this.Runtime = runtime;
            this.InvokeMethodOptions = invokeMethodOptions;
            this.InterfaceVersion = interfaceVersion;
        }

        public IGrainReferenceRuntime Runtime { get; }

        public GrainType GrainType { get; }

        public GrainInterfaceType InterfaceType { get; }

        public InvokeMethodOptions InvokeMethodOptions { get; }

        public ushort InterfaceVersion { get; }
    }

    [Hagar.RegisterSerializer]
    internal class GrainReferenceCodec : GeneralizedReferenceTypeSurrogateCodec<IAddressable, GrainReferenceSurrogate>
    {
        private readonly IGrainFactory _grainFactory;
        public GrainReferenceCodec(IGrainFactory grainFactory, IValueSerializer<GrainReferenceSurrogate> surrogateSerializer) : base(surrogateSerializer)
        {
            _grainFactory = grainFactory;
        }

        public override IAddressable ConvertFromSurrogate(ref GrainReferenceSurrogate surrogate)
        {
            return _grainFactory.GetGrain(surrogate.GrainId, surrogate.GrainInterfaceType);
        }

        public override void ConvertToSurrogate(IAddressable value, ref GrainReferenceSurrogate surrogate)
        {
            var refValue = value.AsReference();
            surrogate = new GrainReferenceSurrogate
            {
                GrainId = refValue.GrainId,
                GrainInterfaceType = refValue.InterfaceType
            };
        }
    }

    [Hagar.RegisterCopier]
    internal class GrainReferenceCopier : IGeneralizedCopier
    {
        public object DeepCopy(object input, CopyContext context) => input;
        public bool IsSupportedType(Type type) => typeof(IAddressable).IsAssignableFrom(type);
    }

    internal class GrainReferenceCodecProvider : ISpecializableCodec
    {
        private readonly IServiceProvider _serviceProvider;
        public GrainReferenceCodecProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public IFieldCodec GetSpecializedCodec(Type type) => (IFieldCodec)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, typeof(TypedGrainReferenceCodec<>).MakeGenericType(type));
        public bool IsSupportedType(Type type) => typeof(IAddressable).IsAssignableFrom(type);
    }

    internal class TypedGrainReferenceCodec<T> : GeneralizedReferenceTypeSurrogateCodec<T, GrainReferenceSurrogate> where T : class, IAddressable
    {
        private readonly IGrainFactory _grainFactory;
        public TypedGrainReferenceCodec(IGrainFactory grainFactory, IValueSerializer<GrainReferenceSurrogate> surrogateSerializer) : base(surrogateSerializer)
        {
            _grainFactory = grainFactory;
        }

        public override T ConvertFromSurrogate(ref GrainReferenceSurrogate surrogate)
        {
            return (T)_grainFactory.GetGrain(surrogate.GrainId, surrogate.GrainInterfaceType);
        }

        public override void ConvertToSurrogate(T value, ref GrainReferenceSurrogate surrogate)
        {
            var refValue = (GrainReference)(object)value.AsReference<T>();
            surrogate = new GrainReferenceSurrogate
            {
                GrainId = refValue.GrainId,
                GrainInterfaceType = refValue.InterfaceType
            };
        }
    }

    [Hagar.GenerateSerializer]
    internal struct GrainReferenceSurrogate
    {
        [Id(1)]
        public GrainId GrainId { get; set; }

        [Id(2)]
        public GrainInterfaceType GrainInterfaceType { get; set; }
    }

    /// <summary>
    /// This is the base class for all typed grain references.
    /// </summary>
    public class GrainReference : IAddressable, IEquatable<GrainReference>
    {
        [NonSerialized]
        private GrainReferenceShared _shared;

        [NonSerialized]
        private IdSpan _key;

        internal GrainReferenceShared Shared => _shared ?? throw new GrainReferenceNotBoundException(this);

        internal IGrainReferenceRuntime Runtime => Shared.Runtime;

        public GrainId GrainId => GrainId.Create(_shared.GrainType, _key);

        public GrainInterfaceType InterfaceType => _shared.InterfaceType;

        /// <summary>Constructs a reference to the grain with the specified Id.</summary>
        protected GrainReference(GrainReferenceShared shared, IdSpan key)
        {
            _shared = shared;
            _key = key;
        }

        /// <summary>Constructs a reference to the grain with the specified ID.</summary>
        internal static GrainReference FromGrainId(GrainReferenceShared shared, GrainId grainId)
        {
            return new GrainReference(shared, grainId.Key);
        }

        public virtual TGrainInterface Cast<TGrainInterface>() where TGrainInterface : IAddressable => (TGrainInterface)_shared.Runtime.Cast(this, typeof(TGrainInterface));

        /// <summary>
        /// Tests this reference for equality to another object.
        /// Two grain references are equal if they both refer to the same grain.
        /// </summary>
        /// <param name="obj">The object to test for equality against this reference.</param>
        /// <returns><c>true</c> if the object is equal to this reference.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as GrainReference);
        }

        public bool Equals(GrainReference other) => other is GrainReference && this.GrainId.Equals(other.GrainId);

        /// <summary> Calculates a hash code for a grain reference. </summary>
        public override int GetHashCode() => this.GrainId.GetHashCode();

        /// <summary>Get a uniform hash code for this grain reference.</summary>
        public uint GetUniformHashCode()
        {
            // GrainId already includes the hashed type code for generic arguments.
            return GrainId.GetUniformHashCode();
        }

        /// <summary>
        /// Compares two references for equality.
        /// Two grain references are equal if they both refer to the same grain.
        /// </summary>
        /// <param name="reference1">First grain reference to compare.</param>
        /// <param name="reference2">Second grain reference to compare.</param>
        /// <returns><c>true</c> if both grain references refer to the same grain (by grain identifier).</returns>
        public static bool operator ==(GrainReference reference1, GrainReference reference2)
        {
            if (reference1 is null) return reference2 is null;

            return reference1.Equals(reference2);
        }

        /// <summary>
        /// Compares two references for inequality.
        /// Two grain references are equal if they both refer to the same grain.
        /// </summary>
        /// <param name="reference1">First grain reference to compare.</param>
        /// <param name="reference2">Second grain reference to compare.</param>
        /// <returns><c>false</c> if both grain references are resolved to the same grain (by grain identifier).</returns>
        public static bool operator !=(GrainReference reference1, GrainReference reference2)
        {
            if (reference1 is null) return !(reference2 is null);

            return !reference1.Equals(reference2);
        }

        /// <summary>
        /// Implemented by generated subclasses to return a constant.
        /// </summary>
        public virtual int InterfaceTypeCode
        {
            get
            {
                throw new InvalidOperationException("Should be overridden by subclass");
            }
        }

        /// <summary>
        /// Return the method name associated with the specified interfaceId and methodId values.
        /// </summary>
        /// <param name="interfaceId">Interface Id</param>
        /// <param name="methodId">Method Id</param>
        /// <returns>Method name string.</returns>
        public virtual string GetMethodName(int interfaceId, int methodId)
        {
            throw new InvalidOperationException("Should be overridden by subclass");
        }

        /// <summary>
        /// Implemented in generated code.
        /// </summary>
        public virtual ushort InterfaceVersion
        {
            get
            {
                throw new InvalidOperationException("Should be overridden by subclass");
            }
        }

        /// <summary>
        /// Return the name of the interface for this GrainReference.
        /// Implemented in Orleans generated code.
        /// </summary>
        public virtual string InterfaceName => InterfaceType.ToStringUtf8();

        /// <summary>Returns a string representation of this reference.</summary>
        public override string ToString() => $"GrainReference:{GrainId}:{InterfaceType}";

        /// <summary>
        /// Called from generated code.
        /// </summary>
        protected void InvokeOneWayMethod(int methodId, object[] arguments, InvokeMethodOptions options = InvokeMethodOptions.None)
        {
            this.Runtime.InvokeOneWayMethod(this, methodId, arguments, options | _shared.InvokeMethodOptions);
        }

        /// <summary>
        /// Called from generated code.
        /// </summary>
        protected Task<T> InvokeMethodAsync<T>(int methodId, object[] arguments, InvokeMethodOptions options = InvokeMethodOptions.None)
        {
            return this.Runtime.InvokeMethodAsync<T>(this, methodId, arguments, options | _shared.InvokeMethodOptions);
        }
    }

    public abstract class NewGrainReference : GrainReference
    {
        protected NewGrainReference(GrainReferenceShared shared, IdSpan key) : base(shared, key)
        {
        }

        public override ushort InterfaceVersion => Shared.InterfaceVersion;

        protected void SendRequest(IResponseCompletionSource callback, IInvokable body) => this.Runtime.SendRequest(this, callback, body, InvokeMethodOptions.None);

        protected void SendOneWay(IResponseCompletionSource callback, IInvokable body) => this.Runtime.SendRequest(this, callback, body, InvokeMethodOptions.OneWay);

        protected void SendRequestWithOptions(IResponseCompletionSource callback, IInvokable body, InvokeMethodOptions options) => this.Runtime.SendRequest(this, callback, body, options);
    }
}
