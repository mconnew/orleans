using System.Threading.Tasks;
using TestExtensions;
using UnitTests.GrainInterfaces;
using UnitTests.Grains;
using Xunit;

namespace DefaultCluster.Tests.General
{
    /// <summary>
    /// Summary description for SimpleGrain
    /// </summary>
    public class SimpleGrainTests : HostedTestClusterEnsureDefaultStarted
    {
        public SimpleGrainTests(DefaultClusterFixture fixture) : base(fixture)
        {
        }

        public ISimpleGrain GetSimpleGrain()
        {
            return this.GrainFactory.GetGrain<ISimpleGrain>(GetRandomGrainId(), SimpleGrain.SimpleGrainNamePrefix);
        }

        [Fact, TestCategory("BVT"), TestCategory("Functional")]
        public async Task SimpleGrainGetGrain()
        {
            ISimpleGrain grain = GetSimpleGrain();
            int ignored = await grain.GetAxB();
        }

        [Fact, TestCategory("BVT"), TestCategory("Functional")]
        public async Task SimpleGrainControlFlow()
        {
            ISimpleGrain grain = GetSimpleGrain();
            
            Task setPromise = grain.SetA(2);
            await setPromise;

            setPromise = grain.SetB(3);
            await setPromise;

            Task<int> intPromise = grain.GetAxB();
            Assert.Equal(6, await intPromise);
        }

        [Fact, TestCategory("BVT"), TestCategory("Functional")]
        public void SimpleGrainControlFlow_Blocking()
        {
            ISimpleGrain grain = GetSimpleGrain();

            // explicitly use .Wait() and .Result to make sure the client does not deadlock in these cases.
            grain.SetA(2).Wait();
            grain.SetB(3).Wait();

            var result = grain.GetAxB().Result;
            Assert.Equal(6, result);
        }

        [Fact, TestCategory("BVT"), TestCategory("Functional")]
        public async Task SimpleGrainDataFlow()
        {
            ISimpleGrain grain = GetSimpleGrain();

            Task setAPromise = grain.SetA(3);
            Task setBPromise = grain.SetB(4);
            await Task.WhenAll(setAPromise, setBPromise);
            var x = await grain.GetAxB();

            Assert.Equal(12, x);
        }

        [Fact(Skip = "Grains with multiple constructors are not supported without being explicitly registered.")]
        [TestCategory("BVT"), TestCategory("Functional")]
        public async Task GettingGrainWithMultipleConstructorsActivesViaDefaultConstructor()
        {
            ISimpleGrain grain = this.GrainFactory.GetGrain<ISimpleGrain>(GetRandomGrainId(), grainClassNamePrefix: MultipleConstructorsSimpleGrain.MultipleConstructorsSimpleGrainPrefix);

            var actual = await grain.GetA();
            Assert.Equal(MultipleConstructorsSimpleGrain.ValueUsedByParameterlessConstructor, actual);
        }
    }
}
