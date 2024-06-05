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
    public class PowerDnsController(IRqliteService rqliteService, ITransactionManager transactionManager, IConfiguration configRoot) : ControllerBase
    {
        private readonly IRqliteService _rqliteService = rqliteService;
        private readonly ITransactionManager _transactionManager = transactionManager;
        private readonly IConfiguration _configRoot = configRoot;

        #region "Not Implemented"

        // GET PowerDNS/lookup/SOA
        [HttpGet("lookup/{qtype}")]
        public IResponse Lookup(string qtype) => NotImplemented();

        #endregion

        #region "GET"

        // GET PowerDNS/getUpdatedMasters
        [HttpGet("getUpdatedMasters")]
        public async Task<IResponse> GetUpdatedMasters()
        {
            try
            {
                using (QueryResult queryResult = await _rqliteService.QueryAsync(GetDomainInfoQuery(kind: "master")))
                {
                    if (queryResult != null && queryResult.Results != null)
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
                using (QueryResult queryResult = await _rqliteService.QueryAsync(GetRecordsQuery(name: q.Replace("*", "%"), match_operator: "LIKE"), ReadConsistencyLevel.None))
                {
                    if (queryResult != null && queryResult.Results != null)
                    {
                        var response = ListResponse.FromValues(queryResult.Results.FirstOrDefault()?.Values);

                        if (response.Result.Count > maxResults)
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

                using (QueryResult queryResult = await _rqliteService.QueryAsync(GetDomainMetaDataQuery(name, true)))
                {
                    if (queryResult != null && queryResult.Results != null)
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
                using (QueryResult queryResult = await _rqliteService.QueryAsync(GetDomainMetaDataQuery(name,false,kind)))
                {
                    if (queryResult != null && queryResult.Results != null)
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
                using (QueryResult queryResult = await _rqliteService.QueryAsync(GetDomainInfoQuery()))
                {
                    if (queryResult != null && queryResult.Results != null)
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

                using (QueryResult queryResult = await _rqliteService.QueryAsync(GetDomainInfoQuery(name)))
                {
                    if (queryResult != null && queryResult.Results != null)
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
                using (QueryResult queryResult = await _rqliteService.QueryAsync(GetRecordsQuery(domain_id)))
                {
                    if (queryResult != null && queryResult.Results != null)
                    {
                        ListResponse response = ListResponse.FromValues(queryResult.Results?.FirstOrDefault()?.Values);

                        // We need to ask Transaction Manager if we need to hook the lookup query
                        if (_transactionManager.Transactions().Count != 0 && _transactionManager.TransactionExistsWith(domain_id))
                        {
                            var transactionRecords = _transactionManager.GetLastTransaction(domain_id)?.Records;

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

                        return response;
                    }

                    throw new NoValuesException();
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
        [HttpGet("lookup/{qname}/{qtype}")]
        public async Task<IResponse> Lookup(string qtype, string qname)
        {

            string query;

            try
            {
                // We need to ask Transaction Manager if we need to hook the lookup query
                if (_transactionManager.Transactions().Count != 0 && _transactionManager.TransactionExistsWith(qname, qtype))
                {
                    Log.Verbose("Found existing Transaction which handles {QName} {QType} => Hooking this query with Transaction Manager", qname, qtype);
                    // returns null when last transaction for this record was DELETE else it returns the record itself
                    IRecord? transactionRecord = _transactionManager.GetLastTransactionRecord(qname, qtype);
                    if (transactionRecord is null)
                    {
                        Log.Verbose("Transaction Manager returned NULL => Record was deleted in Transaction => returning false");
                        throw new NoValuesException();
                    }
                    else
                    {
                        Log.Verbose("Transaction Manager recturned a record => Record was inserted / updated in transaction => returning record");
                        Log.Debug("{TRR}", transactionRecord.ToString());
                        return new ListResponse() { Result = [transactionRecord] };
                    }
                }

                if (qtype == "ANY")
                {
                    query = GetRecordsQuery(name: qname);
                }
                else
                {
                    query = GetRecordsQuery(name: qname, type: qtype);
                }

                using (QueryResult queryResult = await _rqliteService.QueryAsync(query, ReadConsistencyLevel.None))
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

                string query;

                int domain_id = await GetDomainID(name);

                if (value.Length == 0)
                {
                    query = $"DELETE FROM domainmetadata WHERE domain_id={domain_id} AND LOWER(kind)='{kind.ToLower()}'";
                }
                else
                {
                    query = $"SELECT 1 FROM domainmetadata WHERE domain_id={domain_id} AND LOWER(kind)='{kind.ToLower()}'";

                    using (QueryResult queryResult = await _rqliteService.QueryAsync(query))
                    {
                        if (queryResult != null && queryResult.Results != null)
                        {
                            if (queryResult.Results[0]?.Values?[0][0].GetInt32() == 1)
                            {
                                query = $"UPDATE domainmetadata SET content = {value[0]} WHERE domain_id={domain_id} AND LOWER(kind)='{kind.ToLower()}'";
                            }
                            else
                            {
                                query = $"INSERT INTO domainmetadata(domain_id, kind, content) VALUES({domain_id},'{kind}','{value[0]}')";
                            }
                        }
                        else
                        {
                            return new BoolResponse { Result = false };
                        }
                    }
                }

                using (ExecuteResult execResult = await _rqliteService.ExecuteAsync(query))
                {
                    if (execResult is null || execResult.Results is null || execResult.Results.FirstOrDefault()?.RowsAffected <= 0)
                    {
                        return new BoolResponse { Result = false };
                    }
                    else
                    {
                        return new BoolResponse { Result = true };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to update Domain meta data for domain {name} => Kind: {kind} | value: {value}"], Result = false };
            }
        }

        // PATCH PowerDNS/setnotified/1
        [HttpPatch("setnotified/{id}")]
        public async Task<IResponse> SetNotified(int id, [FromForm] int serial)
        {
            try
            {
                using (ExecuteResult execResult = await _rqliteService.ExecuteAsync($"UPDATE domains SET notified_serial={serial} WHERE id={id}"))
                {
                    if (execResult is null || execResult.Results is null || execResult.Results.FirstOrDefault()?.RowsAffected <= 0)
                    {
                        return new BoolResponse { Result = false };
                    }
                    else
                    {
                        return new BoolResponse { Result = true };
                    }
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
                bool result = false;
                List<string> querys = [];
                int transactionid = -1;
 
                if (_transactionManager.Transactions().Exists(x => x.DomainId == domain_id)) { transactionid = _transactionManager.GetLastTransaction(domain_id)?.Id ?? -1; }

                if (rrset.Length > 1)
                {
                    rrset.SkipLast(1).AsParallel().ForAll(x => x.TTL /= 10);
                }

                if (transactionid != -1)
                {
                    if (!_transactionManager.AddTransaction(transactionid, GetDeleteRecordQuery(domain_id, qname, qtype), domain_id, qname, qtype, Enums.PowerDNS.TransactionMode.DELETE))
                    {
                        throw new InvalidOperationException($"Failed to add transaction record to transaction with id {transactionid}");
                    }
                }
                else
                {
                    querys.Add(GetDeleteRecordQuery(domain_id, qname, qtype));
                }

                foreach (Record rr in rrset)
                {
                    rr.DomainId = domain_id;

                    if (transactionid != -1)
                    {
                        if (!_transactionManager.AddTransaction(transactionid, GetInsertRecordQuery(rr), rr, Enums.PowerDNS.TransactionMode.INSERT))
                        {
                            throw new InvalidOperationException($"Failed to add transaction record to transaction with id {transactionid}");
                        }
                    }
                    else
                    {
                        querys.Add(GetInsertRecordQuery(rr));
                    }

                }

                if (querys.Count > 0)
                {

                    using (ExecuteResult execResult = await _rqliteService.ExecuteBulkAsync(querys))
                    {
                        if (execResult.Results is null || execResult.Results.Exists(x => x is null || x.LastInsertId <= 0))
                        {
                            throw new InvalidOperationException("One or more transactions failed to execute!");
                        }
                    }
                }

                result = true;

                return new BoolResponse() { Result = result };

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
                int domain_id = _transactionManager.GetTransaction(trxid).DomainId;

                if (rr.DomainId <= 0) { rr.DomainId = domain_id; }

                if (_transactionManager.AddTransaction(trxid, GetInsertRecordQuery(rr), rr, Enums.PowerDNS.TransactionMode.INSERT))
                {
                    return new BoolResponse { Result = true };
                }
                else
                {
                    return new BoolResponse { Result = false };
                }

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

        // POST PowerDNSAPI/starttransaction/1/example.com
        [HttpPost("starttransaction/{domain_id}/{domain}/{trxid}")]
        public async Task<IResponse> StartTransaction(int domain_id, string domain, int trxid)
        {

            try
            {
                if (domain_id <= 0)
                {
                    domain_id = await GetDomainID(domain);
                }

                return new BoolResponse { Result = _transactionManager.StartTransaction(trxid, domain_id, domain) };

            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to start transaction for domain {domain}"], Result = false };
            }
        }

        // POST PowerDNSAPI/committransaction/1
        [HttpPost("committransaction/{trxid}")]
        public async Task<IResponse> CommitTransaction(int trxid)
        {
            try
            {
                var transactions = _transactionManager.GetTransactionQuerys(trxid);

                if (transactions != null && transactions.Count > 0)
                {

                    using (ExecuteResult execResult = await _rqliteService.ExecuteBulkAsync(transactions))
                    {
                        if (execResult.Results is null || execResult.Results.Exists(x => x is null || x.LastInsertId <= 0))
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
                    _transactionManager.RemoveTransaction(trxid);
                }
                catch (Exception ex)
                {
                    Log.Error(ex,"Failed to remove/cleanup transaction");
                }

            }

        }

        // POST PowerDNSAPI/aborttransaction/1
        [HttpPost("aborttransaction/{trxid}")]
        public IResponse AbortTransaction(int trxid)
        {
            try
            {
                return new BoolResponse { Result = _transactionManager.RemoveTransaction(trxid) };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ErrorMessage}", ex.Message);
                return new BoolResponse { Log = [$"Failed to abort Transaction with id {trxid}"], Result = false };
            }

        }

        #endregion

        #region "Helper"

        private static BoolResponse NotImplemented()
        {
            return new BoolResponse { Log = ["Not implemented"], Result = false };
        }

        private static string GetRecordsQuery(int? domain_id = null,string? name = null, string? type = null, string match_operator = "=")
        {
            string query = "SELECT domain_id,name,type,content,ttl,disabled,auth FROM records";

            if (domain_id != null)
            {
                query = $"{query} WHERE domain_id {match_operator} {domain_id}";
            }

            if (!string.IsNullOrEmpty(name))
            {
                if (query.Contains("WHERE"))
                {
                    query = $"{query} AND LOWER(name) {match_operator} '{name.ToLower()}'";
                }
                else
                {
                    query = $"{query} WHERE LOWER(name) {match_operator} '{name.ToLower()}'";
                }
            }

            if (!string.IsNullOrEmpty(type))
            {
                if (query.Contains("WHERE"))
                {
                    query = $"{query} AND type {match_operator} '{type}'";
                }
                else
                {
                    query = $"{query} WHERE type {match_operator} '{type}'";
                }
            }

            return query;
        }

        private static string GetDomainInfoQuery(string? name = null, string? kind = null)
        {
            string query = $"SELECT id,name,master,last_check,type,notified_serial,account FROM domains";

            if (!string.IsNullOrEmpty(name))
            {
                query = $"{query} WHERE LOWER(name)='{name.ToLower()}'";
            }

            if (!string.IsNullOrEmpty(kind))
            {
                if (query.Contains("WHERE"))
                {
                    query = $"{query} AND LOWER(kind)='{kind.ToLower()}'";
                }
                else
                {
                    query = $"{query} WHERE LOWER(kind)='{kind.ToLower()}'";
                }   
            }

            return query;


        }
        private static string GetDomainMetaDataQuery(string name, bool includeKindColumn = false , string? kind = null)
        {
            List<string> cols = ["content"];

            if (includeKindColumn)
            {
                cols.Add("kind");
            }

            if (string.IsNullOrEmpty(kind))
            {
                return $"SELECT {string.Join(",",cols)} FROM domains,domainmetadata WHERE domains.id=domainmetadata.domain_id AND LOWER(domains.name)='{name.ToLower()}'";
            }
            else
            {
                return $"SELECT {string.Join(",", cols)} FROM domains,domainmetadata WHERE domains.id=domainmetadata.domain_id AND LOWER(domains.name)='{name.ToLower()}' AND LOWER(domainmetadata.kind)='{kind.ToLower()}'";
            }

            
        }
        private static string GetInserDomainQuery(string name)
        {
            return $"INSERT INTO domains (name,master,last_check,type,notified_serial,account) VALUES('{name.ToLower()}','',NULL,'NATIVE',NULL,'')";
        }
        private static string GetInsertRecordQuery(Record rr)
        {
            return $"INSERT INTO records (domain_id,name,type,content,ttl,disabled,ordername,auth) VALUES ({rr.DomainId},'{rr.QName}','{rr.QType}','{rr.Content}',{rr.TTL},{rr.Disabled},NULL,{rr.Auth})";
        }

        private static string GetDeleteRecordQuery(int domain_id, string qname, string qtype)
        {
            string query;

            if (qtype == "ANY")
            {
                query = $"DELETE FROM records WHERE domain_id={domain_id} AND LOWER(name)='{qname.ToLower()}'";
            }
            else
            {
                query = $"DELETE FROM records WHERE domain_id={domain_id} AND (LOWER(name)='{qname.ToLower()}' AND type='{qtype}')";
            }

            return query;
        }

        private async Task<int> GetDomainID(string domainname)
        {
            using (QueryResult queryResult = await _rqliteService.QueryAsync($"SELECT id FROM domains WHERE LOWER(name)='{domainname.ToLower()}'"))
            {

                if (queryResult.Results is null || queryResult.Results.FirstOrDefault()?.Values is null || queryResult.Results.FirstOrDefault()?.Values?.Count <= 0)
                {
                    throw new InvalidOperationException($"Failed to get id for domain name '{domainname}'");
                }

                int? value = queryResult.Results[0]?.Values?[0][0].GetInt32();

                return value is null ? throw new InvalidOperationException($"Failed to get id for domain name '{domainname}'") : (int)value;
            }
        }

        private async Task<IResponse> InsertDomain(string domain)
        {
            try
            {
                string query = GetInserDomainQuery(domain);

                using (ExecuteResult execResult = await _rqliteService.ExecuteAsync(query))
                {

                    if (execResult.Results is null || execResult.Results.Exists(x => x.RowsAffected <= 0))
                    {
                        return new BoolResponse { Result = false };
                    }
                    else
                    {
                        return new BoolResponse { Result = true };
                    }

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
                string query = GetInsertRecordQuery(rr);

                using (ExecuteResult execResult = await _rqliteService.ExecuteAsync(query))
                {

                    if (execResult.Results is null || execResult.Results.Exists(x => x.RowsAffected <= 0))
                    {
                        return new BoolResponse { Result = false };
                    }
                    else
                    {
                        return new BoolResponse { Result = true };
                    }

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
