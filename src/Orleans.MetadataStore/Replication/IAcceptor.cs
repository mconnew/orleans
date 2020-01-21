using System.Threading.Tasks;

namespace Orleans.MetadataStore
{
    // ReSharper disable once TypeParameterCanBeVariant
    public interface IAcceptor<TValue>
    {
        ValueTask<(Ballot, TValue)> GetAcceptedValue();
        ValueTask<PrepareResponse> Prepare(Ballot proposerParentBallot, Ballot ballot);
        ValueTask<AcceptResponse> Accept(Ballot proposerParentBallot, Ballot ballot, TValue value);
    }
}