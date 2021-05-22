using System.Threading.Tasks;

namespace Orleans.MetadataStore
{
    public interface IAcceptor<TValue>
    {
        ValueTask<PrepareResponse<TValue>> Prepare(Ballot proposerParentBallot, Ballot ballot);
        ValueTask<AcceptResponse> Accept(Ballot proposerParentBallot, Ballot ballot, TValue value);
    }
}