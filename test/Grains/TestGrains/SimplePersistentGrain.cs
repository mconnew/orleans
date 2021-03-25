using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using UnitTests.GrainInterfaces;


namespace UnitTests.Grains
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class SimplePersistentGrain_State
    {
        [Hagar.Id(0)]
        public int A { get; set; }
        [Hagar.Id(1)]
        public int B { get; set; }
    }

    /// <summary>
    /// A simple grain that allows to set two arguments and then multiply them.
    /// </summary>
    public class SimplePersistentGrain : Grain<SimplePersistentGrain_State>, ISimplePersistentGrain
    {
        private ILogger logger;
        private Guid version;
        
        public SimplePersistentGrain(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger($"{this.GetType().Name}-{this.IdentityString}");
        }

        public override Task OnActivateAsync()
        {
            logger.Info("Activate.");
            version = Guid.NewGuid();
            return base.OnActivateAsync();
        }
        public Task SetA(int a)
        {
            State.A = a;
            return WriteStateAsync();
        }

        public Task SetA(int a, bool deactivate)
        {
            if(deactivate)
                DeactivateOnIdle();
            return SetA(a);
        }

        public Task SetB(int b)
        {
            State.B = b;
            return WriteStateAsync();
        }

        public Task IncrementA()
        {
            State.A++;
            return WriteStateAsync();
        }

        public Task<int> GetAxB()
        {
            return Task.FromResult(State.A*State.B);
        }

        public Task<int> GetAxB(int a, int b)
        {
            return Task.FromResult(a * b);
        }

        public Task<int> GetA()
        {
            return Task.FromResult(State.A);
        }

        public Task<Guid> GetVersion()
        {
            return Task.FromResult(version);
        }

        public Task<object> GetRequestContext()
        {
            var info = RequestContext.Get("GrainInfo");
            return Task.FromResult(info);
        }

        public Task SetRequestContext(int data)
        {
            RequestContext.Set("GrainInfo", data);
            return Task.CompletedTask;
        }
    }
}
