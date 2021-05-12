using System.Threading.Tasks;

namespace Orleans.MetadataStore
{
    public interface IAcceptor<TValue>
    {
        ValueTask<PrepareResponse> Prepare(Ballot proposerParentBallot, Ballot ballot);
        ValueTask<AcceptResponse> Accept(Ballot proposerParentBallot, Ballot ballot, TValue value);
    }
}