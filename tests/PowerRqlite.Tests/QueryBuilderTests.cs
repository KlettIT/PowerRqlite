using PowerRqlite.Controllers;
using PowerRqlite.Interfaces.PowerDNS;
using PdnsRecord = PowerRqlite.Models.PowerDNS.Record;
using Xunit;

namespace PowerRqlite.Tests
{
    /// <summary>
    /// Pure unit tests for the SQL builders: every user value must end up in the
    /// parameter list, never in the SQL text. No database needed.
    /// </summary>
    public class QueryBuilderTests
    {
        private const string Injection = "'; DROP TABLE records;--";

        [Fact]
        public void GetRecordsQuery_NoFilter_PlainSelect()
        {
            var entry = PowerDnsController.GetRecordsQuery();

            Assert.Equal("SELECT domain_id,name,type,content,ttl,disabled,auth FROM records", entry.Sql);
            Assert.Empty(entry.Args);
        }

        [Fact]
        public void GetRecordsQuery_Filters_BoundAsParameters()
        {
            var entry = PowerDnsController.GetRecordsQuery(domain_id: 5, name: "Www.Example.com", type: "A");

            Assert.Equal("SELECT domain_id,name,type,content,ttl,disabled,auth FROM records WHERE domain_id = ? AND LOWER(name) = ? AND type = ?", entry.Sql);
            Assert.Equal([5, "www.example.com", "A"], entry.Args);
        }

        [Fact]
        public void GetRecordsQuery_LikeWildcard_ValueStaysData()
        {
            // The query builder itself does not transform wildcards; that's done by the caller
            // (SearchRecords uses LikePattern). The parameter value stays as-is here.
            var entry = PowerDnsController.GetRecordsQuery(name: "www.example*", matchOperator: "LIKE");

            Assert.Equal("SELECT domain_id,name,type,content,ttl,disabled,auth FROM records WHERE LOWER(name) LIKE ? ESCAPE '\\'", entry.Sql);
            Assert.Equal(["www.example*"], entry.Args);
        }

        [Theory]
        [InlineData("www.example*", "www.example%")]
        [InlineData("_sip._tcp*", "\\_sip.\\_tcp%")]
        [InlineData("100%*", "100\\%%")]
        [InlineData("a\\b", "a\\\\b")]
        public void LikePattern_OnlyStarIsAWildcard(string q, string expected)
        {
            Assert.Equal(expected, PowerDnsController.LikePattern(q));
        }

        [Fact]
        public void GetRecordsQuery_ExcludeDisabledAndLimit()
        {
            var entry = PowerDnsController.GetRecordsQuery(name: "www.example.com.", includeDisabled: false, limit: 10);

            Assert.Equal("SELECT domain_id,name,type,content,ttl,disabled,auth FROM records WHERE LOWER(name) = ? AND COALESCE(disabled, 0) = 0 LIMIT ?", entry.Sql);
            Assert.Equal(["www.example.com.", 10], entry.Args);
        }

        [Fact]
        public void GetRecordsQuery_Injection_PayloadNeverLeavesTheParameter()
        {
            var entry = PowerDnsController.GetRecordsQuery(name: Injection);

            // The injection payload is never interpolated into the SQL text.
            Assert.DoesNotContain(Injection, entry.Sql);
            Assert.DoesNotContain("DROP TABLE", entry.Sql);
            // The payload is stored as a parameter (lowercased by the query builder for NOCASE lookups).
            Assert.Equal([Injection.ToLower()], entry.Args);
        }

        [Fact]
        public void GetDomainInfoQuery_KindFiltersOnTypeColumn()
        {
            // Regression: the domains table has 'type', not 'kind'.
            var entry = PowerDnsController.GetDomainInfoQuery(kind: "master");

            Assert.Contains("LOWER(type) = ?", entry.Sql);
            Assert.DoesNotContain("LOWER(kind)", entry.Sql);
            Assert.Equal(["master"], entry.Args);
        }

        [Fact]
        public void GetDomainInfoQuery_NameOnly_LowercasedParameter()
        {
            var entry = PowerDnsController.GetDomainInfoQuery(name: "EXAMPLE.com");

            Assert.Equal("SELECT id,name,master,last_check,type,notified_serial,account FROM domains WHERE LOWER(name) = ?", entry.Sql);
            Assert.Equal(["example.com"], entry.Args);
        }

        [Fact]
        public void GetUpdatedMastersQuery_JoinsSoaRecordForSerial()
        {
            var entry = PowerDnsController.GetUpdatedMastersQuery();

            Assert.Contains("LEFT JOIN records r", entry.Sql);
            Assert.Contains("r.type = 'SOA'", entry.Sql);
            Assert.Contains("WHERE LOWER(d.type) = ?", entry.Sql);
            Assert.Equal(["master"], entry.Args);
        }

        [Fact]
        public void GetDomainMetaDataQuery_BoundParameters()
        {
            var entry = PowerDnsController.GetDomainMetaDataQuery("Example.com", includeKindColumn: true, kind: "PRESIGNED");

            Assert.Equal("SELECT content,kind FROM domains,domainmetadata WHERE domains.id=domainmetadata.domain_id AND LOWER(domains.name) = ? AND LOWER(domainmetadata.kind) = ?", entry.Sql);
            Assert.Equal(["example.com", "presigned"], entry.Args);
        }

        [Fact]
        public void GetInserDomainQuery_NameIsDataNotSql()
        {
            var entry = PowerDnsController.GetInserDomainQuery(Injection);

            Assert.Equal("INSERT INTO domains (name,master,last_check,type,notified_serial,account) VALUES (?,'',NULL,'NATIVE',NULL,'')", entry.Sql);
            Assert.Equal([Injection.ToLower()], entry.Args);
        }

        [Fact]
        public void GetInsertRecordQuery_AllColumnsBound()
        {
            var rr = new PdnsRecord
            {
                DomainId = 3,
                QName = Injection,
                QType = "A",
                Content = "10.0.0.1",
                TTL = 300,
                Auth = 1,
                Disabled = true,
            };

            var entry = PowerDnsController.GetInsertRecordQuery(rr);

            Assert.Equal("INSERT INTO records (domain_id,name,type,content,ttl,disabled,ordername,auth) VALUES (?,?,?,?,?,?,NULL,?)", entry.Sql);
            Assert.Equal([3, Injection, "A", "10.0.0.1", 300, 1, 1], entry.Args);
        }

        [Fact]
        public void GetDeleteRecordQuery_AnyType_DeletesWholeQname()
        {
            var entry = PowerDnsController.GetDeleteRecordQuery(7, Injection, "ANY");

            Assert.Equal("DELETE FROM records WHERE domain_id = ? AND LOWER(name) = ?", entry.Sql);
            // The payload is lowercased for the NOCASE lookup.
            Assert.Equal([7, Injection.ToLower()], entry.Args);
        }

        [Fact]
        public void GetDeleteRecordQuery_SpecificType_ThreeParameters()
        {
            var entry = PowerDnsController.GetDeleteRecordQuery(7, "www.example.com", "NS");

            Assert.Equal("DELETE FROM records WHERE domain_id = ? AND LOWER(name) = ? AND type = ?", entry.Sql);
            Assert.Equal([7, "www.example.com", "NS"], entry.Args);
        }

        [Fact]
        public void GetDomainIDQuery_Parameterized()
        {
            var entry = PowerDnsController.GetDomainIDQuery(Injection);

            Assert.Equal("SELECT id FROM domains WHERE LOWER(name) = ?", entry.Sql);
            Assert.Equal([Injection.ToLower()], entry.Args);
        }
    }
}
