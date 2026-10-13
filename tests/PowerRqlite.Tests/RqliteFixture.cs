using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PowerRqlite.Models.rqlite;
using PowerRqlite.Services;
using PowerRqlite.Services.rqlite;
using Xunit;

namespace PowerRqlite.Tests
{
    /// <summary>
    /// One rqlite container; the schema is deployed with the same code path the app uses.
    /// </summary>
    public sealed class RqliteFixture : IAsyncLifetime
    {
        private readonly IContainer rqlite = new ContainerBuilder("rqlite/rqlite:10.6.0")
            .WithPortBinding(4001, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(4001).ForPath("/readyz")))
            .Build();

        public string RqliteUrl { get; private set; } = null!;

        public IRqliteService Service { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await rqlite.StartAsync();
            RqliteUrl = $"http://{rqlite.Hostname}:{rqlite.GetMappedPublicPort(4001)}";

            Service = new RqliteService(new RqliteContext(Options.Create(new RqliteOptions { Url = RqliteUrl })));
            await InitService.DeployAsync(Service);
        }

        public async Task DisposeAsync() => await rqlite.DisposeAsync();
    }

    [CollectionDefinition("rqlite")]
    public class RqliteCollection : ICollectionFixture<RqliteFixture>
    {
    }

    /// <summary>
    /// The real app (src/Program.cs) pointed at a test rqlite.
    /// </summary>
    public sealed class PowerRqliteApp(string rqliteUrl, IDictionary<string, string?>? settings = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["rqliteOptions:Url"] = rqliteUrl,
            }.Concat(settings ?? new Dictionary<string, string?>())));
    }
}
