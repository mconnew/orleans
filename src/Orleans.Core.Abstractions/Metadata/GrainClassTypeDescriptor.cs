using System;
using System.Diagnostics;

namespace Orleans.Metadata
{
    /// <summary>
    /// Describes a grain class.
    /// </summary>
    [DebuggerDisplay("{" + nameof(ClassType) + "}")]
    public class GrainClassTypeDescriptor
    {
        /// <summary>
        /// Initializes an instance of the <see cref="GrainClassTypeDescriptor"/> class.
        /// </summary>
        /// <param name="classType">The grain class type.</param>
        public GrainClassTypeDescriptor(Type classType)
        {
            this.ClassType = classType;
        }

        /// <summary>
        /// Gets the grain class type described by this instance.
        /// </summary>
        public Type ClassType { get; }
    }
}