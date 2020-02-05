using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Orleans.CodeGeneration;
using Orleans.Core;
using Orleans.Metadata.NewGrainRefSystem;
using Orleans.Serialization;

namespace Orleans.Runtime
{
    public sealed class UntypedGrainReference : IGrainReference
    {
        public UntypedGrainReference(GrainId grainId) => this.GrainId = grainId;

        public GrainId GrainId { get; }

        public Type InterfaceType => typeof(IAddressable);
    }

    /// <summary>
    /// This is the base class for all typed grain references.
    /// </summary>
    [Serializable]
    public class GrainReference : IAddressable, IGrainReference, IEquatable<GrainReference>, ISerializable
    {
        [NonSerialized]
        private ImrGrainReferencePrototype prototype;

        [NonSerialized]
        private readonly SpanId key;

        internal bool IsSystemTarget { get { return GrainId.IsSystemTarget(); } }

        internal IGrainReferenceRuntime Runtime => this.prototype?.Runtime;

        /// <summary>
        /// Gets a value indicating whether this instance is bound to a runtime and hence valid for making requests.
        /// </summary>
        internal bool IsBound => this.runtime != null;

        public GrainId GrainId => new GrainId(this.prototype.GrainType, this.key);

        public Type InterfaceType => this.prototype.InterfaceType;

        /// <summary>Constructs a reference to the grain with the specified Id.</summary>
        private GrainReference(ImrGrainReferencePrototype prototype, SpanId key)
        {
            this.prototype = prototype;
            this.key = key;
        }

        /// <summary>
        /// Constructs a copy of a grain reference.
        /// </summary>
        /// <param name="other">The reference to copy.</param>
        protected GrainReference(GrainReference other) : this(other.prototype, other.key)
        {
        }

        protected internal GrainReference(GrainReference other, InvokeMethodOptions invokeMethodOptions)
            : this(other)
        {
        }

        /// <summary>
        /// Binds this instance to a runtime.
        /// </summary>
        /// <param name="prototype">The runtime.</param>
        internal void Bind(ImrGrainReferencePrototype prototype)
        {
            this.prototype = prototype;
        }

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
        
        public bool Equals(GrainReference other)
        {
            if (other is null)
                return false;

            if (!GrainId.Equals(other.GrainId))
            {
                return false;
            }

            return true;
        }

        /// <summary> Calculates a hash code for a grain reference. </summary>
        public override int GetHashCode() => GrainId.GetHashCode();

        /// <summary>Get a uniform hash code for this grain reference.</summary>
        public uint GetUniformHashCode()
        {
            // GrainId already includes the hashed type code for generic arguments.
            return (uint)GrainId.GetHashCode();
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
            if (((object)reference1) == null)
                return ((object)reference2) == null;

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
            if (((object)reference1) == null)
                return ((object)reference2) != null;

            return !reference1.Equals(reference2);
        }

        /// <summary>
        /// Implemented by generated subclasses to return a constant
        /// Implemented in generated code.
        /// </summary>
        public virtual int InterfaceId
        {
            get
            {
                throw new InvalidOperationException("Should be overridden by subclass");
            }
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
        /// Implemented in generated code.
        /// </summary>
        public virtual bool IsCompatible(int interfaceId)
        {
            throw new InvalidOperationException("Should be overridden by subclass");
        }

        /// <summary>
        /// Return the name of the interface for this GrainReference. 
        /// Implemented in Orleans generated code.
        /// </summary>
        public virtual string InterfaceName
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
        /// Called from generated code.
        /// </summary>
        protected void InvokeOneWayMethod(int methodId, object[] arguments, InvokeMethodOptions options = InvokeMethodOptions.None, SiloAddress silo = null)
        {
            this.Runtime.InvokeOneWayMethod(this, methodId, arguments, options | this.prototype.InvokeMethodOptions, silo);
        }

        /// <summary>
        /// Called from generated code.
        /// </summary>
        protected Task<T> InvokeMethodAsync<T>(int methodId, object[] arguments, InvokeMethodOptions options = InvokeMethodOptions.None, SiloAddress silo = null)
        {
            return this.Runtime.InvokeMethodAsync<T>(this, methodId, arguments, options | this.prototype.InvokeMethodOptions, silo);
        }

        private const string GRAIN_REFERENCE_STR = "GrainReference";
        private const string SYSTEM_TARGET_STR = "SystemTarget";
        private const string SYSTEM_TARGET_STR_WITH_EQUAL_SIGN = SYSTEM_TARGET_STR + "=";
        private const string OBSERVER_ID_STR = "ObserverId";
        private const string OBSERVER_ID_STR_WITH_EQUAL_SIGN = OBSERVER_ID_STR + "=";
        private const string GENERIC_ARGUMENTS_STR = "GenericArguments";
        private const string GENERIC_ARGUMENTS_STR_WITH_EQUAL_SIGN = GENERIC_ARGUMENTS_STR + "=";

        /// <summary>Returns a string representation of this reference.</summary>
        public override string ToString()
        {
            return $"{GrainId} ({InterfaceType})";
        }


        /// <summary> Get the key value for this grain, as a string. </summary>
        public string ToKeyString()
        {
            throw new NotImplementedException();
        }
        
        internal static GrainReference FromKeyString(string key, IGrainReferenceRuntime runtime)
        {
            throw new NotImplementedException();
        }

        internal static GrainReference FromKeyInfo(GrainReferenceKeyInfo keyInfo, IGrainReferenceRuntime runtime)
        {
            throw new NotImplementedException();
        }


        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Use the AddValue method to specify serialized values.
            info.AddValue("GrainId", GrainId, typeof(GrainId));
            info.AddValue("InterfaceType", this.InterfaceType, typeof(Type));
        }

        // The special constructor is used to deserialize values. 
        protected GrainReference(SerializationInfo info, StreamingContext context)
        {
            // Reset the property value using the GetValue method.
            var id = (GrainId)info.GetValue("GrainId", typeof(GrainId));
            var type = (Type)info.GetValue("InterfaceType", typeof(Type));

            var serializerContext = context.Context as ISerializerContext;
            var binder = serializerContext?.ServiceProvider.GetService(typeof(Default)) as IGrainReferenceRuntime;
        }
    }
}
