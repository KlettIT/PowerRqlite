using System.Text.Json;
using Xunit;

namespace PowerRqlite.Tests
{
    /// <summary>
    /// Endpoint tests: the real app (in-process TestServer) against a real rqlite container.
    /// Covers the remote-backend protocol surface plus the new deleteDomain/directBackendCmd.
    /// </summary>
    [Collection("rqlite")]
    public class PowerDnsApiTests(RqliteFixture fixture)
    {
        private readonly RqliteFixture fixture = fixture;

        private PowerRqliteApp Factory() => new(fixture.RqliteUrl);

        private async Task<string> GetDomainId(string zone) => (await DomainIdOf(zone)).ToString();

        private static HttpRequestMessage Patch(string url, FormUrlEncodedContent content) =>
            new(HttpMethodPatch, url) { Content = content };

        private static readonly HttpMethod HttpMethodPatch = new("PATCH");

        private static FormUrlEncodedContent RrsetForm(string domainId, string qname, string qtype, string content, int trxid = 0) =>
            new(new Dictionary<string, string>
            {
                ["trxid"] = trxid.ToString(),
                [$"rrset[0].qname"] = qname,
                [$"rrset[0].qtype"] = qtype,
                [$"rrset[0].content"] = content,
                ["rrset[0].ttl"] = "300",
                ["rrset[0].auth"] = "1",
                ["rrset[0].domain_id"] = domainId,
            });

        [Fact]
        public async Task GetDomainInfo_UnknownZone_AutoCreatesIt()
        {
            var zone = $"auto-{Guid.NewGuid():N}.";

            await using var factory = Factory();
            var client = factory.CreateClient();

            var doc = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}"));
            Assert.False(doc.RootElement.GetProperty("result").GetBoolean());

            // Second call: the zone now exists and is returned as domain info.
            doc = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}"));
            Assert.Equal(JsonValueKind.Object, doc.RootElement.GetProperty("result").ValueKind);
            Assert.Contains(zone.TrimEnd('.'), doc.RootElement.GetProperty("result").GetProperty("zone").GetString()!);
        }

        [Fact]
        public async Task ReplaceRRSet_WithoutTransaction_WritesRecordAndLookupFindsIt()
        {
            var zone = $"rrset-{Guid.NewGuid():N}.";

            await using var factory = Factory();
            var client = factory.CreateClient();

            _ = await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}");
            var domainId = await GetDomainId(zone);

            var form = RrsetForm(domainId, $"www.{zone}", "A", "10.20.30.40");
            var replace = JsonDocument.Parse(await (await client.SendAsync(Patch($"/PowerDns/replacerrset/{domainId}/www.{zone}/A", form))).Content.ReadAsStringAsync());
            Assert.True(replace.RootElement.GetProperty("result").GetBoolean());

            var lookup = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/lookup/www.{zone}/A"));
            Assert.Equal(JsonValueKind.Array, lookup.RootElement.GetProperty("result").ValueKind);
            var records = lookup.RootElement.GetProperty("result").EnumerateArray().ToList();
            Assert.Contains(records, r => r.GetProperty("content").GetString() == "10.20.30.40");
        }

        [Fact]
        public async Task SetDomainMetadata_TextValue_IsStoredAndReadBack()
        {
            // Regression: the old code interpolated the value unquoted (SET content = {value}),
            // which broke for any value containing a quote and was injectable.
            var zone = $"meta-{Guid.NewGuid():N}.";

            await using var factory = Factory();
            var client = factory.CreateClient();

            _ = await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}");

            var value = "it's a \"quoted\" value; DROP TABLE domainmetadata; --";
            var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["value"] = value });
            var set = JsonDocument.Parse(await (await client.SendAsync(Patch($"/PowerDns/setdomainmetadata/{zone}/PRESIGNED", form))).Content.ReadAsStringAsync());
            Assert.True(set.RootElement.GetProperty("result").GetBoolean());

            var get = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/getdomainmetadata/{zone}/PRESIGNED"));
            Assert.Equal(value, get.RootElement.GetProperty("result")[0].GetString());

            // The table survived the embedded payload.
            var tables = await fixture.Service.QueryAsync("SELECT name FROM sqlite_master WHERE type='table'");
            Assert.Contains("domainmetadata", tables.Results![0]!.Values!.Select(row => row[0].GetString()!));
        }

        [Fact]
        public async Task SetDomainMetadata_EmptyValue_DeletesMetadata()
        {
            var zone = $"meta-del-{Guid.NewGuid():N}.";

            await using var factory = Factory();
            var client = factory.CreateClient();

            _ = await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}");

            var setForm = new FormUrlEncodedContent(new Dictionary<string, string> { ["value"] = "temp" });
            _ = await (await client.SendAsync(Patch($"/PowerDns/setdomainmetadata/{zone}/PRESIGNED", setForm))).Content.ReadAsStringAsync();

            // Empty value array => PowerDNS semantics: delete the metadata.
            var emptyForm = new FormUrlEncodedContent(new Dictionary<string, string>());
            var del = JsonDocument.Parse(await (await client.SendAsync(Patch($"/PowerDns/setdomainmetadata/{zone}/PRESIGNED", emptyForm))).Content.ReadAsStringAsync());
            Assert.True(del.RootElement.GetProperty("result").GetBoolean());

            var get = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/getdomainmetadata/{zone}/PRESIGNED"));
            Assert.False(get.RootElement.GetProperty("result").GetBoolean());
        }

        [Fact]
        public async Task GetUpdatedMasters_MasterZone_ReturnedWithSoaSerial()
        {
            // Regression: the old query filtered on a 'kind' column that does not exist
            // (the column is 'type'), so getUpdatedMasters silently always failed.
            var name = $"master-{Guid.NewGuid():N}";

            await fixture.Service.ExecuteAsync(
                "INSERT INTO domains (name,master,last_check,type,notified_serial,account) VALUES (?,'1.2.3.4',NULL,'MASTER',0,'')",
                [name]);
            await fixture.Service.ExecuteAsync(
                "INSERT INTO records (domain_id,name,type,content,ttl,auth) VALUES (?,?,?,?,?,1)",
                [await DomainIdOf(name), name, "SOA", "ns1.example. hostmaster.example. 42 900 2400 604800 86400", 3600]);

            await using var factory = Factory();
            var client = factory.CreateClient();

            var doc = JsonDocument.Parse(await client.GetStringAsync("/PowerDns/getUpdatedMasters"));
            var result = doc.RootElement.GetProperty("result");
            Assert.Equal(JsonValueKind.Array, result.ValueKind);

            var match = result.EnumerateArray()
                .First(x => x.GetProperty("zone").GetString()!.Contains(name));
            Assert.Equal(42, match.GetProperty("serial").GetInt32());
            Assert.Equal("MASTER", match.GetProperty("kind").GetString());
        }

        [Fact]
        public async Task DeleteDomain_RemovesZoneAndAllChildRows()
        {
            var zone = $"del-{Guid.NewGuid():N}.";

            await using var factory = Factory();
            var client = factory.CreateClient();

            // zone + records + metadata
            _ = await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}");
            var domainId = await GetDomainId(zone);
            var rrset = RrsetForm(domainId, $@"www.{zone}", "A", "10.0.0.7");
            _ = await (await client.SendAsync(Patch($"/PowerDns/replacerrset/{domainId}/www.{zone}/A", rrset))).Content.ReadAsStringAsync();

            var metaForm = new FormUrlEncodedContent(new Dictionary<string, string> { ["value"] = "x" });
            _ = await (await client.SendAsync(Patch($"/PowerDns/setdomainmetadata/{zone}/PRESIGNED", metaForm))).Content.ReadAsStringAsync();

            var del = JsonDocument.Parse(await (await client.PostAsync($"/PowerDns/deleteDomain/{zone}", new StringContent(""))).Content.ReadAsStringAsync());
            Assert.True(del.RootElement.GetProperty("result").GetBoolean());

            // Zone and all children gone.
            var zoneCount = await fixture.Service.QueryAsync("SELECT COUNT(*) FROM domains WHERE LOWER(name) = ?", [zone]);
            Assert.Equal(0, zoneCount.Results![0]!.Values![0][0].GetInt32());
            var recordCount = await fixture.Service.QueryAsync("SELECT COUNT(*) FROM records WHERE domain_id = ?", [int.Parse(domainId)]);
            Assert.Equal(0, recordCount.Results![0]!.Values![0][0].GetInt32());
            var metaCount = await fixture.Service.QueryAsync("SELECT COUNT(*) FROM domainmetadata WHERE domain_id = ?", [int.Parse(domainId)]);
            Assert.Equal(0, metaCount.Results![0]!.Values![0][0].GetInt32());

            // Unknown domain => false, no error.
            var missing = JsonDocument.Parse(await (await client.PostAsync("/PowerDns/deleteDomain/does-not-exist.example.", new StringContent(""))).Content.ReadAsStringAsync());
            Assert.False(missing.RootElement.GetProperty("result").GetBoolean());
        }

        [Fact]
        public async Task SearchRecords_Wildcard_MatchesSubstrings()
        {
            var zone = $"search-{Guid.NewGuid():N}.";

            await using var factory = Factory();
            var client = factory.CreateClient();

            _ = await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}");
            var domainId = await GetDomainId(zone);
            foreach (var (qname, content) in new[] { ($"alpha.{zone}", "10.1.1.1"), ($"gamma.{zone}", "10.1.1.2"), ($"beta.{zone}", "10.1.1.3") })
            {
                var f = RrsetForm(domainId, qname, "A", content);
                _ = await (await client.SendAsync(Patch($"/PowerDns/replacerrset/{domainId}/{Uri.EscapeDataString(qname)}/A", f))).Content.ReadAsStringAsync();
            }

            var doc = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/searchRecords?q=alpha*&maxResults=100"));
            var names = doc.RootElement.GetProperty("result").EnumerateArray()
                .Select(r => r.GetProperty("qname").GetString()!)
                .ToList();

            Assert.Contains(names, n => n.Contains("alpha"));
            Assert.DoesNotContain(names, n => n.Contains("beta"));
        }

        [Fact]
        public async Task SearchRecords_WithoutMaxResults_ReturnsAllMatches()
        {
            // Regression: an omitted maxResults parameter binds to 0; the old Take(0)
            // silently returned an empty result set for every such call.
            var zone = $"search-nolimit-{Guid.NewGuid():N}.";

            await using var factory = Factory();
            var client = factory.CreateClient();

            _ = await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}");
            var domainId = await GetDomainId(zone);
            foreach (var content in new[] { "10.9.0.1", "10.9.0.2" })
            {
                var f = RrsetForm(domainId, $"s{content.Replace(".", string.Empty)}.{zone}", "A", content);
                _ = await (await client.SendAsync(Patch($"/PowerDns/replacerrset/{domainId}/{Uri.EscapeDataString($"s{content.Replace(".", string.Empty)}.{zone}")}/A", f))).Content.ReadAsStringAsync();
            }

            var doc = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/searchRecords?q=s*.{zone}*"));
            var names = doc.RootElement.GetProperty("result").EnumerateArray().Select(r => r.GetProperty("qname").GetString()!).ToList();

            Assert.Equal(2, names.Count);
        }

        [Fact]
        public async Task DirectBackendCmd_EchoesQueryAndSucceeds()
        {
            await using var factory = Factory();
            var client = factory.CreateClient();

            var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["query"] = "PING" });
            var doc = JsonDocument.Parse(await (await client.PostAsync("/PowerDns/directBackendCmd", form)).Content.ReadAsStringAsync());

            Assert.True(doc.RootElement.GetProperty("result").GetBoolean());
            Assert.Equal("PING", doc.RootElement.GetProperty("log")[0].GetString());
        }

        // Raw replaceRRSet bodies as PowerDNS' HTTP connector writes them for www.{zone} A 192.0.2.1 + 192.0.2.2, TTL 300.
        // 4.x: "auth" without key and no '&' between the records, so record 0 arrives with ttl=3001. 5.x: well-formed.
        [Theory]
        [InlineData("1&rrset[0][content]=192.0.2.1&rrset[0][qclass]=1&rrset[0][qname]=www.{zone}&rrset[0][qtype]=A&rrset[0][ttl]=3001&rrset[1][content]=192.0.2.2&rrset[1][qclass]=1&rrset[1][qname]=www.{zone}&rrset[1][qtype]=A&rrset[1][ttl]=300")]
        [InlineData("rrset[0][auth]=1&rrset[0][content]=192.0.2.1&rrset[0][qclass]=1&rrset[0][qname]=www.{zone}&rrset[0][qtype]=A&rrset[0][ttl]=300&rrset[1][auth]=1&rrset[1][content]=192.0.2.2&rrset[1][qclass]=1&rrset[1][qname]=www.{zone}&rrset[1][qtype]=A&rrset[1][ttl]=300")]
        public async Task ReplaceRRSet_PowerDnsConnectorBody_StoresTtlUnchanged(string body)
        {
            var zone = $"ttl-{Guid.NewGuid():N}.";

            await using var factory = Factory();
            var client = factory.CreateClient();

            _ = await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}");
            var domainId = await GetDomainId(zone);

            var content = new StringContent(body.Replace("{zone}", zone), System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
            var replace = JsonDocument.Parse(await (await client.SendAsync(new HttpRequestMessage(HttpMethodPatch, $"/PowerDns/replacerrset/{domainId}/www.{zone}/A") { Content = content })).Content.ReadAsStringAsync());
            Assert.True(replace.RootElement.GetProperty("result").GetBoolean());

            var stored = await fixture.Service.QueryAsync("SELECT content, ttl FROM records WHERE domain_id = ? ORDER BY content", [int.Parse(domainId)]);
            Assert.Equal([("192.0.2.1", 300), ("192.0.2.2", 300)], stored.Results![0]!.Values!.Select(row => (row[0].GetString(), row[1].GetInt32())).ToArray());
        }

        [Fact]
        public async Task CreateZone_ViaBackendCmd_CreatesSoaAndNs()
        {
            var zone = $"create-{Guid.NewGuid():N}.example";

            await using var factory = Factory();
            var client = factory.CreateClient();

            async Task<bool> Create() => JsonDocument.Parse(await (await client.PostAsync("/PowerDns/directBackendCmd",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["query"] = $"create-zone {zone} ns1.{zone}" }))).Content.ReadAsStringAsync())
                .RootElement.GetProperty("result").GetBoolean();

            Assert.True(await Create());

            var soa = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/lookup/{zone}./SOA"));
            Assert.StartsWith($"a.misconfigured.dns.server.invalid hostmaster.{zone} 0 ", soa.RootElement.GetProperty("result")[0].GetProperty("content").GetString());
            var ns = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/lookup/{zone}./NS"));
            Assert.Equal($"ns1.{zone}", ns.RootElement.GetProperty("result")[0].GetProperty("content").GetString());

            // A second create-zone must not add a second SOA.
            Assert.False(await Create());
        }

        [Fact]
        public async Task CreateZone_ViaBackendCmd_CompletesBareZone()
        {
            // PowerDNS 5.x: a failed 'pdnsutil create-zone' leaves the bare zone that getDomainInfo inserted.
            var zone = $"bare-{Guid.NewGuid():N}.example.";

            await using var factory = Factory();
            var client = factory.CreateClient();

            _ = await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}");

            var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["query"] = $"create-zone {zone}" });
            var doc = JsonDocument.Parse(await (await client.PostAsync("/PowerDns/directBackendCmd", form)).Content.ReadAsStringAsync());
            Assert.True(doc.RootElement.GetProperty("result").GetBoolean());

            var soa = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/lookup/{zone}/SOA"));
            Assert.Single(soa.RootElement.GetProperty("result").EnumerateArray());
            var zoneCount = await fixture.Service.QueryAsync("SELECT COUNT(*) FROM domains WHERE LOWER(name) = ?", [zone]);
            Assert.Equal(1, zoneCount.Results![0]!.Values![0][0].GetInt32());
        }

        [Fact]
        public async Task APILookup_AnswersLikeLookup()
        {
            // PowerDNS 5.x sends APILookup for Web API zone reads and RRset edits.
            var zone = $"apilookup-{Guid.NewGuid():N}.";

            await using var factory = Factory();
            var client = factory.CreateClient();

            _ = await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}");
            var domainId = await GetDomainId(zone);
            _ = await (await client.SendAsync(Patch($"/PowerDns/replacerrset/{domainId}/www.{zone}/A", RrsetForm(domainId, $"www.{zone}", "A", "10.7.7.7")))).Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/APILookup/www.{zone}/ANY"));
            Assert.Equal("10.7.7.7", doc.RootElement.GetProperty("result")[0].GetProperty("content").GetString());
        }

        [Fact]
        public async Task Transaction_Flow_InsertDeleteCommit_IsAtomic()
        {
            var zone = $"trx-{Guid.NewGuid():N}.";

            await using var factory = Factory();
            var client = factory.CreateClient();

            _ = await client.GetStringAsync($"/PowerDns/getDomainInfo/{zone}");
            var domainId = int.Parse(await GetDomainId(zone));

            // startTransaction, two feedRecord calls, commit => both records visible afterwards.
            var start = JsonDocument.Parse(await (await client.PostAsync($"/PowerDns/starttransaction/{domainId}/{zone}/1", new StringContent(""))).Content.ReadAsStringAsync());
            Assert.True(start.RootElement.GetProperty("result").GetBoolean());

            FormUrlEncodedContent Feed(string qname, string content) => new(new Dictionary<string, string>
            {
                ["qname"] = qname,
                ["qtype"] = "A",
                ["content"] = content,
                ["ttl"] = "300",
                ["auth"] = "1",
                ["domain_id"] = "0",
            });

            var f1 = JsonDocument.Parse(await (await client.SendAsync(Patch("/PowerDns/feedrecord/1", Feed($"a.{zone}", "10.5.5.1")))).Content.ReadAsStringAsync());
            var f2 = JsonDocument.Parse(await (await client.SendAsync(Patch("/PowerDns/feedrecord/1", Feed($"b.{zone}", "10.5.5.2")))).Content.ReadAsStringAsync());
            Assert.True(f1.RootElement.GetProperty("result").GetBoolean());
            Assert.True(f2.RootElement.GetProperty("result").GetBoolean());

            var commit = JsonDocument.Parse(await (await client.PostAsync("/PowerDns/committransaction/1", new StringContent(""))).Content.ReadAsStringAsync());
            Assert.True(commit.RootElement.GetProperty("result").GetBoolean());

            var lookup = JsonDocument.Parse(await client.GetStringAsync($"/PowerDns/lookup/a.{zone}/A"));
            Assert.Equal("10.5.5.1", lookup.RootElement.GetProperty("result")[0].GetProperty("content").GetString());
        }

        private async Task<int> DomainIdOf(string name)
        {
            var id = (await fixture.Service.QueryAsync("SELECT id FROM domains WHERE LOWER(name) = ?", [name.ToLower()]))
                .Results![0]!.Values![0][0].GetInt32();
            return id;
        }
    }
}
