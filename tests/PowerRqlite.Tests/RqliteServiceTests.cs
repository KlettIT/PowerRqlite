using PowerRqlite.Models.rqlite;
using PowerRqlite.Services;
using Xunit;

namespace PowerRqlite.Tests
{
    /// <summary>
    /// Integration tests against a real rqlite container: the parameterized service
    /// layer and a proof that hostile input stays data.
    /// </summary>
    [Collection("rqlite")]
    public class RqliteServiceTests(RqliteFixture fixture)
    {
        private readonly RqliteFixture fixture = fixture;
        private const string Injection = "evil'); DELETE FROM domains; --";

        [Fact]
        public async Task Parameterized_Insert_And_Query_RoundTrip()
        {
            var name = $"service-test-{Guid.NewGuid():N}.com";

            await fixture.Service.ExecuteAsync(
                "INSERT INTO domains (name,master,last_check,type,notified_serial,account) VALUES (?,'',NULL,'NATIVE',NULL,'')",
                [name]);

            var result = await fixture.Service.QueryAsync(
                "SELECT id FROM domains WHERE LOWER(name) = ?", [name]);

            Assert.NotEmpty(result.Results![0]!.Values!);
            Assert.True(result.Results[0].Values[0][0].GetInt32() > 0);
        }

        [Fact]
        public async Task Injection_Payload_IsStoredLiterally_NotExecuted()
        {
            // Insert a zone whose name is itself an SQL injection payload.
            await fixture.Service.ExecuteAsync(
                "INSERT INTO domains (name,master,last_check,type,notified_serial,account) VALUES (?,'',NULL,'NATIVE',NULL,'')",
                [Injection]);

            // The payload comes back verbatim as a single row...
            var found = await fixture.Service.QueryAsync(
                "SELECT name FROM domains WHERE LOWER(name) = ?", [Injection.ToLower()]);

            Assert.Single(found.Results![0]!.Values!);
            Assert.Equal(Injection, found.Results[0].Values[0][0].GetString());

            // ...and no statement smuggled in through the name ran: table intact, one row.
            var count = await fixture.Service.QueryAsync("SELECT COUNT(*) FROM domains WHERE LOWER(name) = ?", [Injection.ToLower()]);
            Assert.Equal(1, count.Results![0]!.Values![0][0].GetInt32());

            var tables = await fixture.Service.QueryAsync("SELECT name FROM sqlite_master WHERE type='table'");
            Assert.Contains("domains", tables.Results![0]!.Values!.Select(row => row[0].GetString()!));
        }

        [Fact]
        public async Task DeployAsync_ExistingDatabase_SkipsAndLeavesSchemaIntact()
        {
            // The fixture already deployed the schema into this container; a second
            // deploy must take the "schema present" branch and stay a no-op.
            await InitService.DeployAsync(fixture.Service);

            var tables = await fixture.Service.QueryAsync("SELECT name FROM sqlite_master WHERE type='table'");
            Assert.Contains("domains", tables.Results![0]!.Values!.Select(row => row[0].GetString()!));
            Assert.Contains("records", tables.Results![0]!.Values!.Select(row => row[0].GetString()!));
        }

        [Fact]
        public async Task ExecuteBulkAsync_WithArgs_WritesAllStatements()
        {
            var name = $"bulk-test-{Guid.NewGuid():N}.com";

            await fixture.Service.ExecuteAsync(
                "INSERT INTO domains (name,master,last_check,type,notified_serial,account) VALUES (?,'',NULL,'NATIVE',NULL,'')", [name]);
            var id = (await fixture.Service.QueryAsync("SELECT id FROM domains WHERE LOWER(name) = ?", [name]))
                .Results![0]!.Values![0][0].GetInt32();

            // Two parameterized statements, one atomic bulk.
            await fixture.Service.ExecuteBulkAsync(
                [
                    "INSERT INTO domainmetadata (domain_id,kind,content) VALUES (?,?,?)",
                    "INSERT INTO comments (domain_id,name,type,modified_at,comment) VALUES (?,'x','A',0,?)",
                ],
                [
                    [id, "PRESIGNED", "with quote ' inside"],
                    [id, "comment ' with quote"],
                ]);

            var meta = await fixture.Service.QueryAsync(
                "SELECT content FROM domainmetadata WHERE domain_id = ? AND LOWER(kind) = ?", [id, "presigned"]);
            Assert.Equal("with quote ' inside", meta.Results![0]!.Values![0][0].GetString());

            var comment = await fixture.Service.QueryAsync(
                "SELECT comment FROM comments WHERE domain_id = ?", [id]);
            Assert.Equal("comment ' with quote", comment.Results![0]!.Values![0][0].GetString());
        }
    }
}
