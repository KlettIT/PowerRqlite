
using PowerRqlite.Services.rqlite;
using Serilog;

namespace PowerRqlite.Services
{
    public class InitService(IRqliteService rqliteService) : BackgroundService
    {
        private readonly IRqliteService _rqliteService = rqliteService;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            List<string> schema =
[
   "PRAGMA foreign_keys = 1;",
                "",
                "CREATE TABLE domains (" +
                                "id INTEGER PRIMARY KEY," +
                                "name VARCHAR(255) NOT NULL COLLATE NOCASE," +
                                "master VARCHAR(128) DEFAULT NULL," +
                                "last_check INTEGER DEFAULT NULL," +
                                "type VARCHAR(8) NOT NULL," +
                                "notified_serial INTEGER DEFAULT NULL," +
                                "account VARCHAR(40) DEFAULT NULL," +
                                "options VARCHAR(65535) DEFAULT NULL," +
                                "catalog VARCHAR(255) DEFAULT NULL" +
                                ");",
                "",
                "CREATE TABLE records (" +
                                "id INTEGER PRIMARY KEY," +
                                "domain_id INTEGER DEFAULT NULL," +
                                "name VARCHAR(255) DEFAULT NULL," +
                                "type VARCHAR(10) DEFAULT NULL," +
                                "content VARCHAR(65535) DEFAULT NULL," +
                                "ttl INTEGER DEFAULT NULL," +
                                "prio INTEGER DEFAULT NULL," +
                                "disabled BOOLEAN DEFAULT 0," +
                                "ordername VARCHAR(255)," +
                                "auth BOOL DEFAULT 1, FOREIGN KEY(domain_id) REFERENCES domains(id) ON DELETE CASCADE ON UPDATE CASCADE" +
                                ");",
                "",
                "CREATE TABLE supermasters (" +
                                "ip VARCHAR(64) NOT NULL," +
                                "nameserver VARCHAR(255) NOT NULL COLLATE NOCASE," +
                                "account VARCHAR(40) NOT NULL" +
                                ");",
                "",
                "CREATE TABLE comments (" +
                                "id INTEGER PRIMARY KEY," +
                                "domain_id INTEGER NOT NULL," +
                                "name VARCHAR(255) NOT NULL," +
                                "type VARCHAR(10) NOT NULL," +
                                "modified_at INT NOT NULL," +
                                "account VARCHAR(40) DEFAULT NULL," +
                                "comment VARCHAR(65535) NOT NULL, FOREIGN KEY(domain_id) REFERENCES domains(id) ON DELETE CASCADE ON UPDATE CASCADE" +
                                ");",
                "",
                "CREATE TABLE domainmetadata (" +
                                "id INTEGER PRIMARY KEY," +
                                "domain_id INT NOT NULL," +
                                "kind VARCHAR(32) COLLATE NOCASE," +
                                "content TEXT, FOREIGN KEY(domain_id) REFERENCES domains(id) ON DELETE CASCADE ON UPDATE CASCADE" +
                                ");",
                "",
                "CREATE TABLE cryptokeys (" +
                                "id INTEGER PRIMARY KEY," +
                                "domain_id INT NOT NULL," +
                                "flags INT NOT NULL," +
                                "active BOOL," +
                                "published BOOL DEFAULT 1," +
                                "content TEXT, FOREIGN KEY(domain_id) REFERENCES domains(id) ON DELETE CASCADE ON UPDATE CASCADE" +
                                ");",
                "",
                "CREATE TABLE tsigkeys (" +
                                "id INTEGER PRIMARY KEY" +
                                ");",
            ];

            try
            {
                Log.Debug("Check if we have a empty/new database...");
                var result = await _rqliteService.QueryAsync("SELECT name FROM sqlite_master WHERE type='table'");

                if (result?.Results?[0].Values is null)
                {
                    Log.Debug("Seems like database is empty => deploying schema...");
                    await _rqliteService.ExecuteBulkAsync(schema);
                    Log.Debug("Schmea sucessfully deployed!");
                }
                else
                {
                    Log.Debug("Schema already present, nohting to do");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
            }
        }
    }
}
