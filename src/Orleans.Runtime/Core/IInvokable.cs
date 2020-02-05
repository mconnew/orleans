using Orleans.CodeGeneration;

namespace Orleans.Runtime
{
    /// <summary>
    /// Common internal interface for SystemTarget and ActivationData.
    /// </summary>
    internal interface IInvokable
    {
        // TODO: This interface will not work!! InterfaceId is not a sufficient way to identify an interface/invoker (at least needs generic args)
        IGrainMethodInvoker GetInvoker(GrainTypeManager typeManager, int interfaceId);
    }
}