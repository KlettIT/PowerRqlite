using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PowerRqlite.Tests
{
    /// <summary>
    /// The full stack, driven with the real tools:
    /// dig/nsupdate (bind9 container) -> PowerDNS remote backend (container) -> PowerRqlite (in-process, Kestrel) -> rqlite (container).
    /// PowerDNS runs with tests/docker/powerdns/pdns.conf, the same file docker-compose uses.
    /// </summary>
    public sealed class PowerDnsStack : IAsyncLifetime
    {
        private readonly RqliteFixture rqlite = new();
        private readonly INetwork network = new NetworkBuilder().Build();
        private readonly IContainer dnstools;
        private PowerRqliteApp? app;
        private IContainer? powerdns;

        public PowerDnsStack()
        {
            dnstools = new ContainerBuilder("internetsystemsconsortium/bind9:9.20")
                .WithNetwork(network)
                .WithEntrypoint("sleep")
                .WithCommand("infinity")
                .Build();
        }

        public async Task InitializeAsync()
        {
            await rqlite.InitializeAsync();

            app = new PowerRqliteApp(rqlite.RqliteUrl);
            app.UseKestrel(kestrel => kestrel.ListenAnyIP(0));
            app.StartServer();
            int port = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First()).Port;

            await network.CreateAsync();

            powerdns = new ContainerBuilder(Image)
                .WithNetwork(network)
                .WithNetworkAliases("powerdns")
                .WithPortBinding(8081, true)
                .WithExtraHost("host.docker.internal", "host-gateway")
                .WithResourceMapping(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "docker", "powerdns", "pdns.conf")), "/etc/powerdns/pdns.conf")
                .WithResourceMapping(Encoding.UTF8.GetBytes($"remote-connection-string=http:url=http://host.docker.internal:{port}/PowerDns,timeout=20000\n"), "/etc/powerdns/pdns.d/e2e.conf")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("ready to distribute questions"))
                .Build();

            await Task.WhenAll(powerdns.StartAsync(), dnstools.StartAsync());
        }

        public async Task DisposeAsync()
        {
            await dnstools.DisposeAsync();
            await (powerdns?.DisposeAsync() ?? default);
            await network.DisposeAsync();
            await (app?.DisposeAsync() ?? default);
            await rqlite.DisposeAsync();
        }

        /// <summary>PowerDNS image under test; CI runs the suite once per supported major version.</summary>
        public static string Image => Environment.GetEnvironmentVariable("PDNS_IMAGE") ?? "powerdns/pdns-auth-51:5.1.5";

        public static bool IsPowerDns4 => Image.Contains("pdns-auth-4");

        /// <summary>
        /// Tests pass record names relative to the zone ("www", "@"). pdnsutil 4.x appends the zone itself,
        /// 5.x requires the absolute name, so it is expanded here.
        /// </summary>
        public Task<ExecResult> Pdnsutil(params string[] args)
        {
            if (!IsPowerDns4 && args is ["add-record" or "replace-rrset" or "delete-rrset", string zone, string name, ..])
            {
                args = [.. args];
                args[2] = name == "@" ? zone : $"{name}.{zone}";
            }

            return powerdns!.ExecAsync(["pdnsutil", .. args]);
        }

        /// <summary>PowerDNS Web API (api-key from tests/docker/powerdns/pdns.conf).</summary>
        public HttpClient Api() => new()
        {
            BaseAddress = new Uri($"http://{powerdns!.Hostname}:{powerdns.GetMappedPublicPort(8081)}/api/v1/servers/localhost/"),
            DefaultRequestHeaders = { { "X-API-Key", "powerrqlite-test" } },
        };

        public async Task<string> Dig(params string[] args) => (await dnstools.ExecAsync(["dig", "@powerdns", "+norec", .. args])).Stdout;

        public async Task<ExecResult> NsUpdate(string zone, params string[] updates)
        {
            string file = $"/tmp/{Guid.NewGuid():N}.nsupdate";
            await dnstools.CopyAsync(Encoding.UTF8.GetBytes($"server powerdns\nzone {zone}\n{string.Join("\n", updates)}\nsend\n"), file);
            return await dnstools.ExecAsync(["nsupdate", file]);
        }
    }
}
