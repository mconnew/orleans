using System;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Transactions.Abstractions;

namespace Orleans.Transactions
{
    public class TransactionalStateFactory : ITransactionalStateFactory
    {
        private IGrainContext context;
        public TransactionalStateFactory(IGrainContext context)
        {
            this.context = context;
        }

        public ITransactionalState<TState> Create<TState>(TransactionalStateConfiguration config) where TState : class, new()
        {
            TransactionalState<TState> transactionalState = ActivatorUtilities.CreateInstance<TransactionalState<TState>>(this.context.ActivationServices, config, this.context);
            transactionalState.Participate(context.ObservableLifecycle);
            return transactionalState;
        }

        public static JsonSerializerSettings GetJsonSerializerSettings(IServiceProvider serviceProvider)
        {
            var serializerSettings = OrleansJsonSerializer.GetDefaultSerializerSettings(serviceProvider);
            serializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.None;
            return serializerSettings;
        }
    }
}
