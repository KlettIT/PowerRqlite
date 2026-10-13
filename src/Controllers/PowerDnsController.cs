using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PowerRqlite.Enums.rqlite;
using PowerRqlite.Exceptions;
using PowerRqlite.Interfaces;
using PowerRqlite.Interfaces.PowerDNS;
using PowerRqlite.Models.PowerDNS;
using PowerRqlite.Models.PowerDNS.Responses;
using PowerRqlite.Models.rqlite;
using PowerRqlite.Services.rqlite;
using Serilog;

namespace PowerRqlite.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PowerDnsController(IRqliteService rqliteService, ITransactionManager transactionManager) : ControllerBase
    {
        #region "GET"

        // GET PowerDNS/getUpdatedMasters
        [HttpGet("getUpdatedMasters")]
        public async Task<IResponse> GetUpdatedMasters()
        {
            try
            {
                QueryEntry query = GetUpdatedMastersQuery();

                using (QueryResult queryResult = await rqliteService.QueryAsync(query.Sql, query.Args))
                {
                    if (queryResult.Results != null)
                    {
                        return GetUpdatedMastersResponse.FromValues(queryResult.Results.FirstOrDefault()?.Values);
                    }
                }

                throw new NoValuesException();
            }
            catch (NoValuesException)
            {
                Log.Debug("No masters found");
                return new BoolResponse { Result = false };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = ["Failed to get updated Masters"], Result = false };
            }
        }

        // GET PowerDNS/searchRecords?q=www.example*&maxResults=100
        [HttpGet("searchRecords")]
        public async Task<IResponse> SearchRecords(string q, int maxResults)
        {
            try
            {
                // PowerDNS '*' wildcard => SQL LIKE '%'. Bound as parameter, never concatenated.
                QueryEntry query = GetRecordsQuery(name: q.Replace("*", "%"), matchOperator: "LIKE");

                using (QueryResult queryResult = await rqliteService.QueryAsync(query.Sql, query.Args, ReadConsistencyLevel.None))
                {
                    if (queryResult.Results != null)
                    {
                        ListResponse response = ListResponse.FromValues(queryResult.Results.FirstOrDefault()?.Values);

                        // maxResults omitted (binds to 0) means "no limit" — a bound of 0 would wipe the whole result set.
                        if (maxResults > 0 && response.Result.Count > maxResults)
                        {
                            response.Result = response.Result.Take(maxResults).ToList();
                        }

                        return response;
                    }

                    throw new NoValuesException();
                }
            }
            catch (NoValuesException)
            {
                Log.Debug("No record found for {Query}", q);
                return new BoolResponse { Result = false };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to search for {q}"], Result = false };
            }
        }


        // GET PowerDNS/getalldomainmetadata/www.example.com
        [HttpGet("getalldomainmetadata/{name}")]
        public async Task<IResponse> GetAllDomainMetadata(string name)
        {
            try
            {
                QueryEntry query = GetDomainMetaDataQuery(name, true);

                using (QueryResult queryResult = await rqliteService.QueryAsync(query.Sql, query.Args))
                {
                    if (queryResult.Results != null)
                    {
                        return KeyToArrayResponse.FromValues(queryResult.Results.FirstOrDefault()?.Values);
                    }
                }

                throw new NoValuesException();
            }
            catch (NoValuesException)
            {
                Log.Debug("No metadata records found for domain '{DomainName}'", name);
                return new BoolResponse { Result = false };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to get all DomainMetaData for domain {name}"], Result = false };
            }
        }


        // GET PowerDNS/getdomainmetadata/example.com./PRESIGNED
        [HttpGet("getdomainmetadata/{name}/{kind}")]
        public async Task<IResponse> GetDomainMetadata(string name, string kind)
        {
            try
            {
                QueryEntry query = GetDomainMetaDataQuery(name, false, kind);

                using (QueryResult queryResult = await rqliteService.QueryAsync(query.Sql, query.Args))
                {
                    if (queryResult.Results != null)
                    {
                        return StringArrayResponse.FromValues(queryResult.Results.FirstOrDefault()?.Values);
                    }
                }

                throw new NoValuesException();
            }
            catch (NoValuesException)
            {
                Log.Debug("No metadata record '{Kind}' found for domain '{Name}'", kind, name);
                return new BoolResponse { Result = false };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to get DomainMetaData for domain '{name}' with kind '{kind}'"], Result = false };
            }
        }


        //GET PowerDNS/getAllDomains?includeDisabled = true
        [HttpGet("getAllDomains")]
        public async Task<IResponse> GetAllDomains(bool includeDisabled)
        {
            try
            {
                QueryEntry query = GetDomainInfoQuery();

                using (QueryResult queryResult = await rqliteService.QueryAsync(query.Sql, query.Args))
                {
                    if (queryResult.Results != null)
                    {
                        return AllDomainsResponse.FromValues(queryResult.Results.FirstOrDefault()?.Values);
                    }
                }

                throw new NoValuesException();
            }
            catch (NoValuesException)
            {
                Log.Debug("No domain entries found in database");
                return new BoolResponse { Result = false };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = ["Failed to get all domains"], Result = false };
            }
        }

        // GET PowerDNS/getdomaininfo/example.com.
        [HttpGet("getDomainInfo/{name}")]
        public async Task<IResponse> GetDomainInfo(string name)
        {
            try
            {
                QueryEntry query = GetDomainInfoQuery(name);

                using (QueryResult queryResult = await rqliteService.QueryAsync(query.Sql, query.Args))
                {
                    if (queryResult.Results != null)
                    {
                        return DomainInfoResponse.FromValues(queryResult.Results.FirstOrDefault()?.Values);
                    }
                }

                throw new NoValuesException();
            }
            catch (NoValuesException)
            {
                Log.Debug("No domaininfo records found for domain '{DomainName}'", name);
                Log.Information("Creating new domain/zone '{DomainName}'", name);
                await InsertDomain(name);
                return new BoolResponse { Result = false };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to get DomainInfo for domain '{name}'"], Result = false };
            }
        }

        // GET PowerDNS/list/6/example.com
        [HttpGet("list/{domain_id}/{zonename}")]
        public async Task<IResponse> List(int domain_id, string zonename)
        {

            try
            {
                QueryEntry query = GetRecordsQuery(domain_id);

                using (QueryResult queryResult = await rqliteService.QueryAsync(query.Sql, query.Args))
                {
                    if (queryResult.Results == null) throw new NoValuesException();
                    ListResponse response = ListResponse.FromValues(queryResult.Results?.FirstOrDefault()?.Values);

                    // We need to ask Transaction Manager if we need to hook the lookup query
                    if (transactionManager.Transactions().Count == 0 ||
                        !transactionManager.TransactionExistsWith(domain_id)) return response;

                    List<TransactionRecord>? transactionRecords = transactionManager.GetLastTransaction(domain_id)?.Records;

                    Log.Verbose("Found existing Transaction which handles domain_id {DomainId} => Hooking this query with Transaction Manager", domain_id);

                    transactionRecords?.ForEach(record =>
                    {
                        switch (record.TransactionMode)
                        {
                            case Enums.PowerDNS.TransactionMode.DELETE:
                                response.Result.RemoveAll(x => x.DomainId == record.DomainId && x.QName == record.QName && x.QType == record.QType);
                                break;
                            case Enums.PowerDNS.TransactionMode.INSERT:
                                response.Result.Add(record);
                                break;
                        }
                    });

                    return response;

                }
            }
            catch (NoValuesException)
            {
                Log.Debug("No records found in zone '{Zone}'", zonename);
                return new BoolResponse { Result = false };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to list zone '{zonename}' with domain_id {domain_id}"], Result = false };
            }

        }

        // GET PowerDNS/lookup/www.example.com./ANY
        // APILookup (PowerDNS 5.x, Web API) is a lookup that may include disabled records; this lookup never filters them.
        [HttpGet("lookup/{qname}/{qtype}")]
        [HttpGet("APILookup/{qname}/{qtype}")]
        public async Task<IResponse> Lookup(string qtype, string qname)
        {
            try
            {
                // We need to ask Transaction Manager if we need to hook the lookup query
                if (transactionManager.Transactions().Count != 0 && transactionManager.TransactionExistsWith(qname, qtype))
                {
                    Log.Verbose("Found existing Transaction which handles {QName} {QType} => Hooking this query with Transaction Manager", qname, qtype);
                    // returns null when last transaction for this record was DELETE else it returns the record itself
                    IRecord? transactionRecord = transactionManager.GetLastTransactionRecord(qname, qtype);
                    if (transactionRecord is null)
                    {
                        Log.Verbose("Transaction Manager returned NULL => Record was deleted in Transaction => returning false");
                        throw new NoValuesException();
                    }
                    else
                    {
                        Log.Verbose("Transaction Manager returned a record => Record was inserted / updated in transaction => returning record");
                        Log.Debug("{TRR}", transactionRecord.ToString());
                        return new ListResponse() { Result = [transactionRecord] };
                    }
                }

                QueryEntry query = qtype == "ANY" ? GetRecordsQuery(name: qname) : GetRecordsQuery(name: qname, type: qtype);

                using (QueryResult queryResult = await rqliteService.QueryAsync(query.Sql, query.Args, ReadConsistencyLevel.None))
                {
                    if (queryResult.Results is null || queryResult.Results.Count == 0)
                    {
                        throw new NoValuesException();
                    }

                    return ListResponse.FromValues(queryResult.Results.Single().Values);
                }
            }
            catch (NoValuesException)
            {
                Log.Verbose("No record found for {QName}|{QType}", qname, qtype);
                return new BoolResponse { Result = false };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to lookup {qname}|{qtype}"], Result = false };
            }
        }


        #endregion

        #region "PATCH"

        // PATCH PowerDNS/setdomainmetadata/example.com/PRESIGNED
        [HttpPatch("setdomainmetadata/{name}/{kind}")]
        public async Task<IResponse> SetDomainMetadata(string name, string kind, [FromForm] string[] value)
        {
            try
            {
                int domain_id = await GetDomainID(name);

                QueryEntry query = value.Length == 0
                    ? new QueryEntry("DELETE FROM domainmetadata WHERE domain_id = ? AND LOWER(kind) = ?", [domain_id, kind.ToLower()])
                    : new QueryEntry("SELECT 1 FROM domainmetadata WHERE domain_id = ? AND LOWER(kind) = ?", [domain_id, kind.ToLower()]);

                if (value.Length > 0)
                {
                    using (QueryResult queryResult = await rqliteService.QueryAsync(query.Sql, query.Args))
                    {
                        // First-time set has no metadata row yet => INSERT, existing row => UPDATE. Value is bound, never interpolated.
                        bool exists = queryResult.Results is not null &&
                                     queryResult.Results.Count > 0 &&
                                     queryResult.Results[0]?.Values is { Count: > 0 };

                        query = exists
                            ? new QueryEntry("UPDATE domainmetadata SET content = ? WHERE domain_id = ? AND LOWER(kind) = ?", [value[0], domain_id, kind.ToLower()])
                            : new QueryEntry("INSERT INTO domainmetadata (domain_id, kind, content) VALUES (?, ?, ?)", [domain_id, kind, value[0]]);
                    }
                }

                using (ExecuteResult execResult = await rqliteService.ExecuteAsync(query.Sql, query.Args))
                {
                    if (execResult.Results is null || execResult.Results.FirstOrDefault()?.RowsAffected <= 0)
                    {
                        return new BoolResponse { Result = false };
                    }

                    return new BoolResponse { Result = true };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to update Domain meta data for domain {name} => Kind: {kind} | value: {string.Join("|", value)}"], Result = false };
            }
        }

        // PATCH PowerDNS/setnotified/1
        [HttpPatch("setnotified/{id}")]
        public async Task<IResponse> SetNotified(int id, [FromForm] int serial)
        {
            try
            {
                QueryEntry query = new("UPDATE domains SET notified_serial = ? WHERE id = ?", [serial, id]);

                using (ExecuteResult execResult = await rqliteService.ExecuteAsync(query.Sql, query.Args))
                {
                    if (execResult.Results is null || execResult.Results.FirstOrDefault()?.RowsAffected <= 0)
                    {
                        return new BoolResponse { Result = false };
                    }

                    return new BoolResponse { Result = true };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to update notified_serial for domain_id {id}"], Result = false };
            }
        }

        // PATCH PowerDNS/replacerrset/2/replace.example.com/A
        [HttpPatch("replacerrset/{domain_id}/{qname}/{qtype}")]
        public async Task<IResponse> ReplaceRRSet(int domain_id, string qname, string qtype, [FromForm] int trxid, [FromForm] Record[] rrset)
        {
            try
            {
                List<QueryEntry> queries = [];
                int transactionid = -1;

                if (transactionManager.Transactions().Exists(x => x.DomainId == domain_id)) { transactionid = transactionManager.GetLastTransaction(domain_id)?.Id ?? -1; }

                // PowerDNS 4.x' HTTP connector writes the next record's "auth" bool keyless and without '&' right after
                // the previous "ttl" (ttl=300 + "1" => 3001), so every TTL but the last carries one extra digit.
                // 5.x sends "rrset[i][auth]=1" and separates the records; there the TTLs are already correct.
                bool legacyEncoding = !Request.Form.Keys.Any(k => k.EndsWith("[auth]", StringComparison.Ordinal) || k.EndsWith(".auth", StringComparison.Ordinal));

                if (legacyEncoding && rrset.Length > 1)
                {
                    rrset.SkipLast(1).AsParallel().ForAll(x => x.TTL /= 10);
                }

                QueryEntry deleteQuery = GetDeleteRecordQuery(domain_id, qname, qtype);

                if (transactionid != -1)
                {
                    if (!transactionManager.AddTransaction(transactionid, deleteQuery, domain_id, qname, qtype, Enums.PowerDNS.TransactionMode.DELETE))
                    {
                        throw new InvalidOperationException($"Failed to add transaction record to transaction with id {transactionid}");
                    }
                }
                else
                {
                    queries.Add(deleteQuery);
                }

                foreach (Record rr in rrset)
                {
                    rr.DomainId = domain_id;

                    QueryEntry insertQuery = GetInsertRecordQuery(rr);

                    if (transactionid != -1)
                    {
                        if (!transactionManager.AddTransaction(transactionid, insertQuery, rr, Enums.PowerDNS.TransactionMode.INSERT))
                        {
                            throw new InvalidOperationException($"Failed to add transaction record to transaction with id {transactionid}");
                        }
                    }
                    else
                    {
                        queries.Add(insertQuery);
                    }
                }

                if (queries.Count > 0)
                {
                    using (ExecuteResult execResult = await rqliteService.ExecuteBulkAsync(queries.Select(x => x.Sql).ToList(), queries.Select(x => x.Args).ToArray()))
                    {
                        // INSERTs always affect exactly one row; a DELETE with no matching rows is valid (replacing a not-yet-existing RRset).
                        if (execResult.Results is null ||
                            execResult.Results.Select((r, i) => (r, i)).Any(p => p.r is null || (queries[p.i].Sql.StartsWith("INSERT") && p.r!.RowsAffected <= 0)))
                        {
                            throw new InvalidOperationException("One or more transactions failed to execute!");
                        }
                    }
                }

                return new BoolResponse() { Result = true };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to replace rrset for record '{qname}' for domain_id {domain_id}"], Result = false };
            }
        }

        // PATCH PowerDNS/feedrecord/1370416133
        [HttpPatch("feedrecord/{trxid}")]
        public IResponse FeedRecord(int trxid, [FromForm] Record rr)
        {
            try
            {
                int domainId = transactionManager.GetTransaction(trxid).DomainId;

                if (rr.DomainId <= 0) { rr.DomainId = domainId; }

                QueryEntry query = GetInsertRecordQuery(rr);

                return transactionManager.AddTransaction(trxid, query, rr, Enums.PowerDNS.TransactionMode.INSERT) ? new BoolResponse { Result = true } : new BoolResponse { Result = false };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to feed/Insert record {rr.QName}"], Result = false };
            }
        }

        // PATCH PowerDNS/feedrecord/
        [HttpPatch("feedrecord")]
        public async Task<IResponse> FeedRecord([FromForm] Record rr)
        {
            try
            {
                if (rr.DomainId <= 0)
                {
                    throw new InvalidOperationException("DomainId must be set!");
                }

                return await InsertRecord(rr);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to feed/Insert record {rr.QName}"], Result = false };
            }
        }

        #endregion

        #region "POST"

        // POST PowerDNS/starttransaction/1/example.com
        [HttpPost("starttransaction/{domain_id}/{domain}/{trxid}")]
        public async Task<IResponse> StartTransaction(int domain_id, string domain, int trxid)
        {
            try
            {
                if (domain_id <= 0)
                {
                    domain_id = await GetDomainID(domain);
                }

                return new BoolResponse { Result = transactionManager.StartTransaction(trxid, domain_id, domain) };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to start transaction for domain {domain}"], Result = false };
            }
        }

        // POST PowerDNS/committransaction/1
        [HttpPost("committransaction/{trxid}")]
        public async Task<IResponse> CommitTransaction(int trxid)
        {
            try
            {
                List<QueryEntry> transactions = transactionManager.GetTransactionQuerys(trxid);

                if (transactions.Count > 0)
                {
                    using (ExecuteResult execResult = await rqliteService.ExecuteBulkAsync(transactions.Select(x => x.Sql).ToList(), transactions.Select(x => x.Args).ToArray()))
                    {
                        // Same rule as ReplaceRRSet: only INSERTs must affect a row.
                        if (execResult.Results is null ||
                            execResult.Results.Select((r, i) => (r, i)).Any(p => p.r is null || (transactions[p.i].Sql.StartsWith("INSERT") && p.r!.RowsAffected <= 0)))
                        {
                            throw new InvalidOperationException("One or more transactions failed to execute!");
                        }
                    }

                    return new BoolResponse { Result = true };
                }
                else
                {
                    throw new InvalidOperationException("Transactions were null");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to commit Transaction with id {trxid}"], Result = false };
            }
            finally
            {
                try
                {
                    transactionManager.RemoveTransaction(trxid);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to remove/cleanup transaction");
                }
            }
        }

        // POST PowerDNS/aborttransaction/1
        [HttpPost("aborttransaction/{trxid}")]
        public IResponse AbortTransaction(int trxid)
        {
            try
            {
                return new BoolResponse { Result = transactionManager.RemoveTransaction(trxid) };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to abort Transaction with id {trxid}"], Result = false };
            }
        }

        // POST PowerDNS/deleteDomain/example.com.
        // PowerRqlite extension: PowerDNS' remote backend module does not send a
        // zone deletion method (see Backend::deleteDomain default in PowerDNS),
        // so zones are deleted through this endpoint instead.
        [HttpPost("deleteDomain/{name}")]
        public async Task<IResponse> DeleteDomain(string name)
        {
            try
            {
                // PowerDNS stores zones absolute ("example.com."); accept the name with or without the dot.
                int domain_id = await GetDomainID(name.TrimEnd('.') + ".");

                // Explicit deletes keep the zone deletion working even without
                // SQLite's per-connection foreign_keys pragma. One bulk = one transaction.
                List<string> queries =
                [
                    "DELETE FROM records WHERE domain_id = ?",
                    "DELETE FROM domainmetadata WHERE domain_id = ?",
                    "DELETE FROM comments WHERE domain_id = ?",
                    "DELETE FROM cryptokeys WHERE domain_id = ?",
                    "DELETE FROM domains WHERE id = ?",
                ];

                object?[] args = [domain_id, domain_id, domain_id, domain_id, domain_id];

                using (ExecuteResult execResult = await rqliteService.ExecuteBulkAsync(queries, queries.Select(_ => args).ToArray()))
                {
                    if (execResult.Results is null || execResult.Results.LastOrDefault()?.RowsAffected <= 0)
                    {
                        return new BoolResponse { Result = false, Log = [$"Domain '{name}' not found, nothing deleted."] };
                    }

                    Log.Information("Deleted zone '{Zone}' (domain_id {DomainId})", name, domain_id);
                    return new BoolResponse { Result = true };
                }
            }
            catch (InvalidOperationException)
            {
                Log.Debug("DeleteDomain: domain '{Domain}' does not exist", name);
                return new BoolResponse { Result = false };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to delete domain '{name}'"], Result = false };
            }
        }

        // POST PowerDNS/directBackendCmd  (pdnsutil backend-cmd remote "delete-zone example.com")
        [HttpPost("directBackendCmd")]
        public async Task<IResponse> DirectBackendCmd([FromForm] string query)
        {
            Log.Debug("directBackendCmd: {Query}", query);

            switch (query.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                // 'pdnsutil delete-zone' never reaches a remote backend (no deleteDomain upstream), so it is offered here.
                case ["delete-zone", string zone]:
                    return await DeleteDomain(zone);

                // PowerDNS 5.x refuses 'pdnsutil create-zone' for the remote backend (no CAP_CREATE), so it is offered here too.
                case ["create-zone", string zone]:
                    return await CreateZone(zone, null);

                case ["create-zone", string zone, string nameserver]:
                    return await CreateZone(zone, nameserver);

                default:
                    return new BoolResponse { Log = [query], Result = true };
            }
        }

        // Same result as 'pdnsutil create-zone ZONE [NSNAME]' with PowerDNS' default SOA.
        private async Task<IResponse> CreateZone(string name, string? nameserver)
        {
            string zone = name.TrimEnd('.') + ".";

            try
            {
                // On PowerDNS 5.x a failed 'pdnsutil create-zone' has already inserted the bare zone through getDomainInfo,
                // so a zone without SOA is completed instead of rejected.
                int domain_id;

                try
                {
                    domain_id = await GetDomainID(zone);
                }
                catch (InvalidOperationException)
                {
                    if (await InsertDomain(zone) is not BoolResponse { Result: true })
                    {
                        return new BoolResponse { Result = false, Log = [$"Failed to create zone '{zone}'"] };
                    }

                    domain_id = await GetDomainID(zone);
                }

                QueryEntry soaQuery = GetRecordsQuery(domain_id, zone, "SOA");

                using (QueryResult queryResult = await rqliteService.QueryAsync(soaQuery.Sql, soaQuery.Args))
                {
                    if (queryResult.Results?.FirstOrDefault()?.Values is { Count: > 0 })
                    {
                        return new BoolResponse { Result = false, Log = [$"Zone '{zone}' exists already"] };
                    }
                }

                string apex = zone.TrimEnd('.');
                List<QueryEntry> queries =
                [
                    GetInsertRecordQuery(new Record { DomainId = domain_id, QName = zone, QType = "SOA", Content = $"a.misconfigured.dns.server.invalid hostmaster.{apex} 0 10800 3600 604800 3600", TTL = 3600, Auth = 1 }),
                ];

                if (!string.IsNullOrEmpty(nameserver))
                {
                    queries.Add(GetInsertRecordQuery(new Record { DomainId = domain_id, QName = zone, QType = "NS", Content = nameserver.TrimEnd('.'), TTL = 3600, Auth = 1 }));
                }

                using (ExecuteResult execResult = await rqliteService.ExecuteBulkAsync(queries.Select(x => x.Sql).ToList(), queries.Select(x => x.Args).ToArray()))
                {
                    if (execResult.Results is null || execResult.Results.Exists(x => x is null || x.RowsAffected <= 0))
                    {
                        return new BoolResponse { Result = false, Log = [$"Failed to create zone '{zone}'"] };
                    }
                }

                Log.Information("Created zone '{Zone}' (domain_id {DomainId})", zone, domain_id);
                return new BoolResponse { Result = true };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to create zone '{zone}'"], Result = false };
            }
        }

        // Unimplemented methods and the root-zone lookup ("/lookup/%2e/SOA" is normalized to "/lookup/SOA")
        // must answer {"result":false}; an empty 404 makes PowerDNS abort with "Cannot parse JSON reply".
        [AcceptVerbs("GET", "POST", "PATCH", "PUT", "DELETE")]
        [Route("{**path}")]
        public IResponse NotImplemented(string path)
        {
            Log.Debug("Not implemented: {Path}", path);
            return new BoolResponse { Result = false };
        }

        #endregion

        #region "Helper"

        internal static QueryEntry GetRecordsQuery(int? domain_id = null, string? name = null, string? type = null, string matchOperator = "=")
        {
            List<string> where = [];
            List<object?> args = [];

            if (domain_id != null)
            {
                where.Add($"domain_id {matchOperator} ?");
                args.Add(domain_id);
            }

            if (!string.IsNullOrEmpty(name))
            {
                where.Add($"LOWER(name) {matchOperator} ?");
                args.Add(name.ToLower());
            }

            if (!string.IsNullOrEmpty(type))
            {
                where.Add($"type {matchOperator} ?");
                args.Add(type);
            }

            string sql = "SELECT domain_id,name,type,content,ttl,disabled,auth FROM records";
            sql += where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty;

            return new QueryEntry(sql, [.. args]);
        }

        internal static QueryEntry GetDomainInfoQuery(string? name = null, string? kind = null)
        {
            List<string> where = [];
            List<object?> args = [];

            if (!string.IsNullOrEmpty(name))
            {
                where.Add("LOWER(name) = ?");
                args.Add(name.ToLower());
            }

            if (!string.IsNullOrEmpty(kind))
            {
                // 'kind' is the domains 'type' column (native/master/slave)
                where.Add("LOWER(type) = ?");
                args.Add(kind.ToLower());
            }

            string sql = "SELECT id,name,master,last_check,type,notified_serial,account FROM domains";
            sql += where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty;

            return new QueryEntry(sql, [.. args]);
        }

        /// <summary>
        /// Master zones including their current SOA serial (from the SOA record),
        /// so PowerDNS can compare it with notified_serial for NOTIFY decisions.
        /// </summary>
        internal static QueryEntry GetUpdatedMastersQuery(string kind = "master")
        {
            const string sql =
                "SELECT d.id, d.name, d.master, d.last_check, d.type, d.notified_serial, d.account, r.content " +
                "FROM domains d LEFT JOIN records r ON r.domain_id = d.id AND r.type = 'SOA' AND LOWER(r.name) = LOWER(d.name) " +
                "WHERE LOWER(d.type) = ?";

            return new QueryEntry(sql, [kind.ToLower()]);
        }

        internal static QueryEntry GetDomainMetaDataQuery(string name, bool includeKindColumn = false, string? kind = null)
        {
            List<string> cols = ["content"];

            if (includeKindColumn)
            {
                cols.Add("kind");
            }

            string sql = $"SELECT {string.Join(",", cols)} FROM domains,domainmetadata WHERE domains.id=domainmetadata.domain_id AND LOWER(domains.name) = ?";
            List<object?> args = [name.ToLower()];

            if (!string.IsNullOrEmpty(kind))
            {
                sql += " AND LOWER(domainmetadata.kind) = ?";
                args.Add(kind.ToLower());
            }

            return new QueryEntry(sql, [.. args]);
        }

        internal static QueryEntry GetInserDomainQuery(string name)
        {
            return new QueryEntry(
                "INSERT INTO domains (name,master,last_check,type,notified_serial,account) VALUES (?,'',NULL,'NATIVE',NULL,'')",
                [name.ToLower()]);
        }

        internal static QueryEntry GetInsertRecordQuery(Record rr)
        {
            return new QueryEntry(
                "INSERT INTO records (domain_id,name,type,content,ttl,disabled,ordername,auth) VALUES (?,?,?,?,?,?,NULL,?)",
                [rr.DomainId, rr.QName, rr.QType, rr.Content, rr.TTL, rr.Disabled ? 1 : 0, rr.Auth]);
        }

        internal static QueryEntry GetDeleteRecordQuery(int domain_id, string qname, string qtype)
        {
            string sql = qtype == "ANY"
                ? "DELETE FROM records WHERE domain_id = ? AND LOWER(name) = ?"
                : "DELETE FROM records WHERE domain_id = ? AND LOWER(name) = ? AND type = ?";

            object?[] args = qtype == "ANY"
                ? [domain_id, qname.ToLower()]
                : [domain_id, qname.ToLower(), qtype];

            return new QueryEntry(sql, args);
        }

        internal static QueryEntry GetDomainIDQuery(string domainname)
        {
            return new QueryEntry("SELECT id FROM domains WHERE LOWER(name) = ?", [domainname.ToLower()]);
        }

        private async Task<int> GetDomainID(string domainname)
        {
            QueryEntry query = GetDomainIDQuery(domainname);

            using (QueryResult queryResult = await rqliteService.QueryAsync(query.Sql, query.Args))
            {
                if (queryResult.Results is null || queryResult.Results.FirstOrDefault()?.Values is null || queryResult.Results.FirstOrDefault()?.Values?.Count <= 0)
                {
                    throw new InvalidOperationException($"Failed to get id for domain name '{domainname}'");
                }

                int? value = queryResult.Results[0]?.Values?[0][0].GetInt32();

                return value ?? throw new InvalidOperationException($"Failed to get id for domain name '{domainname}'");
            }
        }

        private async Task<IResponse> InsertDomain(string domain)
        {
            try
            {
                QueryEntry query = GetInserDomainQuery(domain);

                using (ExecuteResult execResult = await rqliteService.ExecuteAsync(query.Sql, query.Args))
                {
                    if (execResult.Results is null || execResult.Results.Exists(x => x.RowsAffected <= 0))
                    {
                        return new BoolResponse { Result = false };
                    }

                    return new BoolResponse { Result = true };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [ex.Message], Result = false };
            }
        }

        private async Task<IResponse> InsertRecord(Record rr)
        {
            try
            {
                QueryEntry query = GetInsertRecordQuery(rr);

                using (ExecuteResult execResult = await rqliteService.ExecuteAsync(query.Sql, query.Args))
                {
                    if (execResult.Results is null || execResult.Results.Exists(x => x.RowsAffected <= 0))
                    {
                        return new BoolResponse { Result = false };
                    }

                    return new BoolResponse { Result = true };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [ex.Message], Result = false };
            }
        }

        #endregion
    }
}
