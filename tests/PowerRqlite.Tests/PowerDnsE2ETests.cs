using DotNet.Testcontainers.Containers;
using Xunit;

namespace PowerRqlite.Tests
{
    /// <summary>
    /// End-to-end: pdnsutil, dig and nsupdate against a real PowerDNS whose remote backend is PowerRqlite.
    /// Every test works on its own zone, so the tests do not depend on each other.
    /// </summary>
    public class PowerDnsE2ETests(PowerDnsStack stack) : IClassFixture<PowerDnsStack>
    {
        private static async Task<string> Ok(Task<ExecResult> run)
        {
            ExecResult result = await run;
            Assert.True(result.ExitCode == 0, $"exit code {result.ExitCode}\n{result.Stdout}\n{result.Stderr}");
            return result.Stdout;
        }

        // backend-cmd works with every PowerDNS version; 'pdnsutil create-zone' only up to 4.x (see CreateZone_ViaPdnsutil).
        private async Task<string> CreateZone()
        {
            string zone = $"e2e-{Guid.NewGuid():N}.test";
            Assert.Equal("1", (await Ok(stack.Pdnsutil("backend-cmd", "remote", $"create-zone {zone} ns1.{zone}"))).Trim());
            return zone;
        }

        [Fact]
        public async Task CreateZone_ViaPdnsutil()
        {
            string zone = $"e2e-{Guid.NewGuid():N}.test";
            ExecResult create = await stack.Pdnsutil("create-zone", zone, $"ns1.{zone}");

            if (PowerDnsStack.IsPowerDns4)
            {
                Assert.Equal(0, create.ExitCode);
            }
            else
            {
                // Upstream: 5.x requires CAP_CREATE, which the remote backend does not report. The bare zone that
                // getDomainInfo inserted along the way is completed by backend-cmd.
                Assert.Contains("None of the configured backends support zone creation", create.Stderr);
                Assert.Equal("1", (await Ok(stack.Pdnsutil("backend-cmd", "remote", $"create-zone {zone} ns1.{zone}"))).Trim());
            }

            Assert.Contains($"hostmaster.{zone}.", Assert.Single(await DigShort(zone, "SOA")));
            Assert.Equal([$"ns1.{zone}."], await DigShort(zone, "NS"));
            await Ok(stack.Pdnsutil("check-zone", zone));
        }

        private async Task<string[]> DigShort(string name, string type) =>
            (await stack.Dig("+short", name, type)).Split('\n', StringSplitOptions.RemoveEmptyEntries).Order().ToArray();

        // Answer lines "www.example. 300 IN A 192.0.2.1" as (ttl, rdata).
        private async Task<(string Ttl, string Data)[]> DigAnswers(string name, string type) =>
            (await stack.Dig("+noall", "+answer", name, type)).Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split((char[]?)null, 5, StringSplitOptions.RemoveEmptyEntries))
                .Select(f => (f[1], f[4]))
                .OrderBy(a => a.Item2)
                .ToArray();

        [Fact]
        public async Task CreateZone_IsListedAndServesSoa()
        {
            string zone = await CreateZone();

            Assert.Contains(zone, await Ok(stack.Pdnsutil("list-all-zones")));
            Assert.Contains($"hostmaster.{zone}.", Assert.Single(await DigShort(zone, "SOA")));
        }

        [Theory]
        [InlineData("www", "A", "192.0.2.10")]
        [InlineData("www", "AAAA", "2001:db8::10")]
        [InlineData("alias", "CNAME", "target.example.")]
        [InlineData("sub", "NS", "ns.sub.example.")]
        [InlineData("@", "MX", "10 mail.example.")]
        [InlineData("@", "TXT", "\"v=spf1 -all\"")]
        [InlineData("_sip._tcp", "SRV", "10 5 5060 sip.example.")]
        public async Task AddRecord_IsServedViaDns(string name, string type, string content)
        {
            string zone = await CreateZone();
            string fqdn = name == "@" ? zone : $"{name}.{zone}";

            await Ok(stack.Pdnsutil("add-record", zone, name, type, "300", content));

            // +answer +authority: a delegation (NS below the apex) is answered as a referral.
            Assert.Contains(content, await stack.Dig("+noall", "+answer", "+authority", fqdn, type));
        }

        [Fact]
        public async Task AddRecord_ToExistingRrset_KeepsAllRecordsAndTtls()
        {
            // Regression for the PowerDNS HTTP connector quirk that appends a digit to every TTL but the last of an RRset.
            string zone = await CreateZone();

            await Ok(stack.Pdnsutil("add-record", zone, "www", "A", "300", "192.0.2.1"));
            await Ok(stack.Pdnsutil("add-record", zone, "www", "A", "300", "192.0.2.2"));

            Assert.Equal([("300", "192.0.2.1"), ("300", "192.0.2.2")], await DigAnswers($"www.{zone}", "A"));
        }

        [Fact]
        public async Task EditRecord_ReplaceRrsetThenDeleteRrset()
        {
            string zone = await CreateZone();
            await Ok(stack.Pdnsutil("add-record", zone, "www", "A", "300", "192.0.2.1"));

            await Ok(stack.Pdnsutil("replace-rrset", zone, "www", "A", "600", "192.0.2.2", "192.0.2.3"));
            Assert.Equal([("600", "192.0.2.2"), ("600", "192.0.2.3")], await DigAnswers($"www.{zone}", "A"));

            await Ok(stack.Pdnsutil("delete-rrset", zone, "www", "A"));
            Assert.Empty(await DigShort($"www.{zone}", "A"));
        }

        [Fact]
        public async Task CheckAllZones_Passes()
        {
            string zone = await CreateZone();
            await Ok(stack.Pdnsutil("add-record", zone, "www", "A", "300", "192.0.2.1"));
            await Ok(stack.Pdnsutil("add-record", zone, "alias", "CNAME", "300", $"www.{zone}."));
            await Ok(stack.Pdnsutil("add-record", zone, "sub", "NS", "300", $"ns.sub.{zone}."));
            await Ok(stack.Pdnsutil("add-record", zone, "ns.sub", "A", "300", "192.0.2.53"));

            // Checks every zone in the backend, including the ones the other tests created.
            await Ok(stack.Pdnsutil("check-zone", zone));
            await Ok(stack.Pdnsutil("check-all-zones"));
        }

        [Fact]
        public async Task DeleteZone_ViaBackendCmd_RemovesZone()
        {
            string zone = await CreateZone();
            await Ok(stack.Pdnsutil("add-record", zone, "www", "A", "300", "192.0.2.1"));

            // Upstream gap: the remote backend has no deleteDomain, so 'pdnsutil delete-zone' always fails.
            Assert.NotEqual(0, (await stack.Pdnsutil("delete-zone", zone)).ExitCode);
            Assert.Contains(zone, await Ok(stack.Pdnsutil("list-all-zones")));

            Assert.Equal("1", (await Ok(stack.Pdnsutil("backend-cmd", "remote", $"delete-zone {zone}"))).Trim());

            Assert.DoesNotContain(zone, await Ok(stack.Pdnsutil("list-all-zones")));
            Assert.Contains("status: REFUSED", await stack.Dig($"www.{zone}", "A"));
        }

        [Fact]
        public async Task Dig_UnknownName_NxDomain()
        {
            string zone = await CreateZone();

            Assert.Contains("status: NXDOMAIN", await stack.Dig($"missing.{zone}", "A"));
        }

        [Fact]
        public async Task NsUpdate_AddReplaceDelete()
        {
            string zone = await CreateZone();
            string dyn = $"dyn.{zone}.";
            long Serial(string[] soa) => long.Parse(Assert.Single(soa).Split(' ')[2]);
            long serialBefore = Serial(await DigShort(zone, "SOA"));

            await Ok(stack.NsUpdate(zone, $"update add {dyn} 300 A 198.51.100.1", $"update add {dyn} 300 A 198.51.100.2", $"update add {dyn} 300 TXT \"hello\""));
            Assert.Equal(["198.51.100.1", "198.51.100.2"], await DigShort(dyn, "A"));
            Assert.Equal(["\"hello\""], await DigShort(dyn, "TXT"));

            // The usual "change the address" update: drop the RRset and add the new record in one packet.
            await Ok(stack.NsUpdate(zone, $"update delete {dyn} A", $"update add {dyn} 300 A 198.51.100.3"));
            Assert.Equal(["198.51.100.3"], await DigShort(dyn, "A"));

            await Ok(stack.NsUpdate(zone, $"update delete {dyn} A 198.51.100.3", $"update delete {dyn} TXT"));
            Assert.Contains("status: NXDOMAIN", await stack.Dig(dyn, "A"));

            Assert.True(Serial(await DigShort(zone, "SOA")) > serialBefore, "DNS UPDATE must increase the SOA serial");
        }
    }
}
