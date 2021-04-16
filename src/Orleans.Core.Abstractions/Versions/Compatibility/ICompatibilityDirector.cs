using System;

namespace Orleans.Versions.Compatibility
{
    public interface ICompatibilityDirector
    {
        bool IsCompatible(ushort requestedVersion, ushort currentVersion);
    }

    [Serializable]
    [Orleans.GenerateSerializer]
    public abstract class CompatibilityStrategy
    {
    }
}