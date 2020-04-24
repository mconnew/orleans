using System.Threading.Tasks;

namespace Orleans.Runtime
{
    public interface IGrainActivator
    {
        object CreateInstance(IGrainContext context);

        ValueTask DisposeInstance(IGrainContext context, object instance);
    }
}