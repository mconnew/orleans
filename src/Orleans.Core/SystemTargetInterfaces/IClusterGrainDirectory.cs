using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Orleans.GrainDirectory;
using Orleans.Runtime;
using System.Collections.Generic;

namespace Orleans.SystemTargetInterfaces
{
    internal enum ActivationResponseStatus
    {
        Pass,
        Failed,
        Faulted
    }

    /// <summary>
    /// Response message used by Global Single Instance Protocol
    /// </summary>
    [Serializable]
    [Orleans.GenerateSerializer]
    internal class RemoteClusterActivationResponse
    {
        public static readonly RemoteClusterActivationResponse Pass = new RemoteClusterActivationResponse(ActivationResponseStatus.Pass);

        public RemoteClusterActivationResponse(ActivationResponseStatus responseStatus)
        {
            this.ResponseStatus = responseStatus;
        }

        [Orleans.Id(1)]
        public ActivationResponseStatus ResponseStatus { get; private set; }
        [Orleans.Id(2)]
        public AddressAndTag ExistingActivationAddress { get; set; }
        [Orleans.Id(3)]
        public string ClusterId { get; set; }
        [Orleans.Id(4)]
        public bool Owned { get; set; }
        [Orleans.Id(5)]
        public Exception ResponseException { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("[");
            sb.Append(ResponseStatus.ToString());
            if (ExistingActivationAddress.Address != null) {
                sb.Append(" ");
                sb.Append(ExistingActivationAddress.Address);
                sb.Append(" ");
                sb.Append(ClusterId);
            }
            if (Owned)
            {
                sb.Append(" owned");
            }
            if (ResponseException != null)
            {
                sb.Append(" ");
                sb.Append(ResponseException.GetType().Name);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
