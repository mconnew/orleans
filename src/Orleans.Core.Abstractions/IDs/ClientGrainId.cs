using System;
using System.Runtime.CompilerServices;

namespace Orleans.Runtime
{
    public static class ClientGrainId
    {
        public static GrainId Create() => GrainId.Create(GrainTypePrefix.ClientGrainType, Guid.NewGuid().ToString("N"));
        public static GrainId Create(string name)
        {
            if (name.Contains("+")) throw new ArgumentException("Client name cannot contain '+' symbols");
            return GrainId.Create(GrainTypePrefix.ClientGrainType, name);
        }

        public static GrainId CreateObserverId(GrainId clientId)
        {
            if (!clientId.Type.IsClient() || clientId.Key.ToStringUtf8().IndexOf('+') >= 0)
            {
                ThrowInvalidGrainId(clientId);
            }

            return GrainId.Create(clientId.Type, clientId.Key + "+" + Guid.NewGuid().ToString("N"));
        }

        public static bool IsObserverId(this GrainId grainId) => grainId.Type.IsClient() && grainId.Key.ToStringUtf8().IndexOf('+') >= 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidGrainId(GrainId grainId)
        {
            throw new ArgumentException($"GrainId {grainId} is in an incorrect format.");
        }

        public static bool TryGetClientId(GrainId id, out GrainId clientId)
        {
            if (!id.Type.IsClient())
            {
                clientId = default;
                return false;
            }

            // For client side objects, strip the observer id.
            var key = id.Key.ToStringUtf8();
            if (key.IndexOf('+') is int index && index >= 0)
            {
                key = key.Substring(0, index);
                clientId = GrainId.Create(id.Type, key);
                return true;
            }

            clientId = id;
            return true;
        }

        public static bool TryGetObserverId(GrainId grainId, out Guid observerId)
        {
            if (!grainId.Type.IsClient())
            {
                observerId = default;
                return false;
            }

            var key = grainId.Key.ToStringUtf8();
            if (key.IndexOf('+') is int index && index >= 0)
            {
                key = key.Substring(index + 1);
                observerId = Guid.Parse(key);
                return true;
            }

            return false;
        }
    }
}
