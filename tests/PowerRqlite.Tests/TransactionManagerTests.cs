using PowerDNS = PowerRqlite.Enums.PowerDNS;
using PowerRqlite.Interfaces.PowerDNS;
using PdnsRecord = PowerRqlite.Models.PowerDNS.Record;
using PowerRqlite.Models.rqlite;
using PowerRqlite.Services.PowerDNS;
using Xunit;

namespace PowerRqlite.Tests
{
    /// <summary>
    /// Pure unit tests for the TransactionManager. No database, no HTTP:
    /// start / feed / lookup / remove semantics of in-flight transactions.
    /// </summary>
    public class TransactionManagerTests
    {
        private readonly TransactionManager tm = new();

        private static QueryEntry Entry(string sql) => new(sql, []);

        private static PdnsRecord Rr(string qname, string qtype, string content, int domainId = 7) =>
            new() { DomainId = domainId, QName = qname, QType = qtype, Content = content, TTL = 300, Auth = 1 };

        [Fact]
        public void StartTransaction_NewId_True_DuplicateId_False()
        {
            Assert.True(tm.StartTransaction(1, 7, "example.com"));
            Assert.False(tm.StartTransaction(1, 8, "other.com"));
            Assert.Single(tm.Transactions());
        }

        [Fact]
        public void AddRecord_UnknownTransaction_False()
        {
            var rr = Rr("a.example.com", "A", "10.0.0.1");
            Assert.False(tm.AddTransaction(99, Entry("INSERT ..."), rr, PowerDNS.TransactionMode.INSERT));
        }

        [Fact]
        public void AddRecord_KnownTransaction_StoresQueryAndRecord()
        {
            tm.StartTransaction(1, 7, "example.com");

            bool added = tm.AddTransaction(1, Entry("INSERT ..."), Rr("a.example.com", "A", "10.0.0.1"), PowerDNS.TransactionMode.INSERT);

            Assert.True(added);
            var t = tm.GetTransaction(1);
            Assert.Single(t.Queries);
            var record = Assert.Single(t.Records);
            Assert.Equal(PowerDNS.TransactionMode.INSERT, record.TransactionMode);
            Assert.Equal("a.example.com", record.QName);
        }

        [Fact]
        public void GetLastTransactionRecord_InsertMode_ReturnsRecord()
        {
            tm.StartTransaction(1, 7, "example.com");
            _ = tm.AddTransaction(1, Entry("..."), Rr("a.example.com", "A", "10.0.0.1"), PowerDNS.TransactionMode.INSERT);

            IRecord? found = tm.GetLastTransactionRecord("a.example.com", "A");

            Assert.NotNull(found);
            Assert.Equal("10.0.0.1", found!.Content);
        }

        [Fact]
        public void GetLastTransactionRecord_DeleteMode_ReturnsNull()
        {
            // A DELETE in a transaction means the record is hidden => lookup must report "none".
            tm.StartTransaction(1, 7, "example.com");
            _ = tm.AddTransaction(1, Entry("DELETE ..."), 7, "a.example.com", "A", PowerDNS.TransactionMode.DELETE);

            Assert.Null(tm.GetLastTransactionRecord("a.example.com", "A"));
        }

        [Fact]
        public void GetLastTransactionRecord_AnyQType_ReturnsLastRecordForQname()
        {
            tm.StartTransaction(1, 7, "example.com");
            _ = tm.AddTransaction(1, Entry("..."), Rr("a.example.com", "A", "10.0.0.1"), PowerDNS.TransactionMode.INSERT);
            _ = tm.AddTransaction(1, Entry("..."), Rr("a.example.com", "AAAA", "2001::1"), PowerDNS.TransactionMode.INSERT);

            IRecord? found = tm.GetLastTransactionRecord("a.example.com", "ANY");

            Assert.NotNull(found);
            Assert.Equal("AAAA", found!.QType);
        }

        [Fact]
        public void GetLastTransactionRecord_NoMatchingTransaction_Null()
        {
            Assert.Null(tm.GetLastTransactionRecord("nobody.example.com", "A"));
        }

        [Fact]
        public void GetLastTransaction_MatchesByDomainIdCaseInsensitiveDomain()
        {
            tm.StartTransaction(1, 7, "example.com");
            tm.StartTransaction(2, 8, "EXAMPLE.COM");

            Assert.Equal(8, tm.GetLastTransaction(8)?.DomainId);
            Assert.Equal(8, tm.GetLastTransaction("example.com")?.DomainId);
        }

        [Fact]
        public void GetTransactionQueries_ReturnsFedQueries()
        {
            tm.StartTransaction(1, 7, "example.com");
            _ = tm.AddTransaction(1, Entry("S1"), Rr("a", "A", "1"), PowerDNS.TransactionMode.INSERT);
            _ = tm.AddTransaction(1, Entry("S2"), Rr("b", "A", "2"), PowerDNS.TransactionMode.INSERT);

            Assert.Equal(["S1", "S2"], tm.GetTransactionQuerys(1).Select(q => q.Sql));
        }

        [Fact]
        public void GetTransaction_Unknown_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => tm.GetTransaction(42));
        }

        [Fact]
        public void RemoveTransaction_Known_True_Unknown_Throws()
        {
            tm.StartTransaction(1, 7, "example.com");

            Assert.True(tm.RemoveTransaction(1));
            Assert.Empty(tm.Transactions());
            Assert.Throws<InvalidOperationException>(() => tm.RemoveTransaction(1));
        }

        [Fact]
        public void TransactionExistsWith_QueryAndDomain_BothDirections()
        {
            Assert.False(tm.TransactionExistsWith("a.example.com", "A"));
            Assert.False(tm.TransactionExistsWith(7));

            tm.StartTransaction(1, 7, "example.com");
            _ = tm.AddTransaction(1, Entry("..."), Rr("a.example.com", "A", "1"), PowerDNS.TransactionMode.INSERT);

            Assert.True(tm.TransactionExistsWith("a.example.com", "A"));
            Assert.True(tm.TransactionExistsWith(7));
        }
    }
}
