using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Orleans.MetadataStore.Tests
{
    public static class DynamicOptionsExtensions
    {
        public static void AddDynamicOptions<T>(this IServiceCollection services, string name = "") where T : class
        {
            services.AddOptions();

            var configureOptions = new DynamicConfigureOptions<T>(name);
            services.AddSingleton(configureOptions);
            services.AddSingleton<IOptionsChangeTokenSource<T>>(configureOptions);
            services.AddSingleton<IConfigureOptions<T>>(configureOptions);
        }

        public class DynamicConfigureOptions<T> : IConfigureNamedOptions<T>, IOptionsChangeTokenSource<T> where T : class
        {
            private readonly string matchName;
            private CancellationTokenSource reloadToken = new CancellationTokenSource();

            internal DynamicConfigureOptions(string name)
            {
                this.matchName = name;
            }

            public Action<T> ConfigureOptions { get; set; }

            public void Reload()
            {
                var previousToken = Interlocked.Exchange(ref this.reloadToken, new CancellationTokenSource());
                previousToken.Cancel();
            }

            void IConfigureOptions<T>.Configure(T options) => this.ConfigureOptions?.Invoke(options);

            IChangeToken IOptionsChangeTokenSource<T>.GetChangeToken() => new CancellationChangeToken(reloadToken.Token);

            string IOptionsChangeTokenSource<T>.Name { get; }

            void IConfigureNamedOptions<T>.Configure(string name, T options)
            {
                if (string.Equals(this.matchName, name))
                {
                    this.ConfigureOptions?.Invoke(options);
                }
            }
        }
    }
}
