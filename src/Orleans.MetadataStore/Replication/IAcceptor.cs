using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Orleans.MetadataStore
{
    public interface IAcceptor<TValue>
    {
        ValueTask<PrepareResponse<TValue>> Prepare(Ballot proposerParentBallot, Ballot ballot);
        ValueTask<AcceptResponse> Accept(Ballot proposerParentBallot, Ballot ballot, TValue value);
    }

    public interface IVolatileAcceptor<TValue>
    {
        PrepareResponse<TValue> Prepare(Ballot proposerParentBallot, Ballot ballot);
        AcceptResponse Accept(Ballot proposerParentBallot, Ballot ballot, TValue value);
    }
}