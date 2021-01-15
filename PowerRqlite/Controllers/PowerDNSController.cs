using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PowerRqlite.Exceptions;
using PowerRqlite.Models.PowerDNS;
using PowerRqlite.Models.PowerDNS.Responses;
using PowerRqlite.Models.rqlite;
using PowerRqlite.Models.rqlite.enums;
using PowerRqlite.Services.PowerDNS;
using PowerRqlite.Services.rqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PowerRqlite.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PowerDNSController : ControllerBase
    {

        private readonly ILogger<PowerDNSController> _logger;
        private readonly IrqliteService _rqliteService;
        private readonly ITransactionManager _transactionManager;
        private readonly IConfiguration _configRoot;

        public PowerDNSController(ILogger<PowerDNSController> logger, IrqliteService rqliteService, ITransactionManager transactionManager, IConfiguration configRoot)
        {
            _logger = logger;
            _rqliteService = rqliteService;
            _transactionManager = transactionManager;
            _configRoot = configRoot;
            
        }


        // GET PowerDNS/list/6/example.com
        [HttpGet("list/{domain_id}/{zonename}")]
        public async Task<IResponse> List(int domain_id, string zonename)
        {

            try
            {
                using (QueryResult queryResult = await _rqliteService.QueryAsync($"SELECT domain_id,name,type,content,ttl,disabled,auth FROM records WHERE domain_id={domain_id}"))
                {
                    ListResponse response = ListResponse.FromValues(queryResult.Results.FirstOrDefault().Values);

                    // We need to ask Transaction Manager if we need to hook the lookup query
                    if (_transactionManager.Transactions().Any() && _transactionManager.TransactionExistsWith(domain_id))
                    {
                            
                        List<IRecord> recordsToDelete = new List<IRecord>();
                        var transactionRecords = _transactionManager.GetLastTransaction(domain_id).Records;

                        _logger.LogInformation($"Found existing Transaction which handles domain_id {domain_id} => Hooking this query with Transaction Manager");

                        Parallel.ForEach(transactionRecords, record =>
                        {
                            switch (record.TransactionMode)
                            {
                                case Models.PowerDNS.Enums.TransactionMode.DELETE:
                                    response.result.RemoveAll(x => x.domain_id == record.domain_id && x.qname == record.qname && x.qtype == record.qtype);
                                    break;
                                case Models.PowerDNS.Enums.TransactionMode.INSERT:
                                    response.result.Add(record);
                                    break;
                            }
                        });

                        return response;
                    }
                    else
                    {
                        return response;
                    }
                }
            }
            catch (NoValuesException)
            {
                _logger.LogDebug($"No records found in zone '{zonename}'");
                return new BoolResponse { result = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to list zone '{zonename}' with domain_id {domain_id}" }, result = false};
            }
      
        }

        // GET PowerDNS/lookup/SOA - what the heck is this!?
        [HttpGet("lookup/{qtype}")]
        public IResponse Lookup(string qtype)
        {
            return new BoolResponse { result = false };
        }

        // GET PowerDNS/lookup/www.example.com./ANY
        [HttpGet("lookup/{qname}/{qtype}")]
        public async Task<IResponse> Lookup(string qname, string qtype)
        {

            string query;

            try
            {

                bool removeTrailingDot = _configRoot.GetValue<bool>("RemoveTrailingDot");

                if (removeTrailingDot && qname.EndsWith("."))
                {
                    qname = qname.Remove(qname.Length - 1);
                }

                // We need to ask Transaction Manager if we need to hook the lookup query
                if (_transactionManager.Transactions().Any() && _transactionManager.TransactionExistsWith(qname, qtype))
                {
                    _logger.LogInformation($"Found existing Transaction which handles {qname} {qtype} => Hooking this query with Transaction Manager");
                    // returns null when last transaction for this record was DELETE else it returns the record itself
                    IRecord transactionRecord = _transactionManager.GetLastTransactionRecord(qname, qtype);
                    if (transactionRecord is null)
                    {
                        _logger.LogInformation("Transaction Manager returned NULL => Record was deleted in Transaction => returning false");
                        throw new NoValuesException(); 
                    }
                    else
                    {
                        _logger.LogInformation("Transaction Manager recturned a record => Record was inserted / updated in transaction => returning record");
                        _logger.LogDebug(transactionRecord.ToString());
                        return new LookupResponse() { result = new List<IRecord>() { transactionRecord } };
                    }
                }

                if (qtype == "ANY")
                {
                    query = $"SELECT domain_id,name,type,content,ttl,disabled,auth FROM records WHERE LOWER(name)='{qname.ToLower()}'";
                }
                else
                {
                    query = $"SELECT domain_id,name,type,content,ttl,disabled,auth FROM records WHERE LOWER(name)='{qname.ToLower()}' AND type='{qtype}'";
                }

                using (QueryResult queryResult = await _rqliteService.QueryAsync(query, ReadConsistencyLevel.None))
                {
                    return LookupResponse.FromValues(queryResult.Results.FirstOrDefault().Values);
                }
            }
            catch (NoValuesException)
            {
                _logger.LogDebug($"No record found for {qname}|{qtype}");
                return new BoolResponse { result = false };
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to lookup {qname}|{qtype}" }, result = false };
            }
        }

        // GET PowerDNS/getbeforeandafternamesabsolute/0/www.example.com
        [HttpGet("getbeforeandafternamesabsolute/{id}/{qname}")]
        public IResponse getBeforeAndAfterNamesAbsolute(int id, string qname)
        {
            return new BoolResponse { Log = new List<string>() { "Not implemented" }, result = false };
        }

        // GET PowerDNS/getalldomainmetadata/www.example.com
        [HttpGet("getalldomainmetadata/{name}")]
        public async Task<IResponse> getAllDomainMetadata(string name)
        {
            try
            {
                bool removeTrailingDot = _configRoot.GetValue<bool>("RemoveTrailingDot");

                if (removeTrailingDot && name.EndsWith("."))
                {
                    name = name.Remove(name.Length - 1);
                }

                using (QueryResult queryResult = await _rqliteService.QueryAsync($"SELECT kind,content FROM domains,domainmetadata WHERE domains.id=domainmetadata.domain_id AND LOWER(domains.name)='{name.ToLower()}'"))
                {
                    return KeyToArrayResponse.FromValues(queryResult.Results.FirstOrDefault().Values);
                }
            }
            catch (NoValuesException)
            {
                _logger.LogDebug($"No metadata records found for domain '{name}'");
                return new BoolResponse { result = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to get all DomainMetaData for domain {name}" }, result = false };
            }
        }


        // GET PowerDNS/getdomainmetadata/example.com./PRESIGNED
        [HttpGet("getdomainmetadata/{name}/{kind}")]
        public async Task<IResponse> getDomainMetadata(string name, string kind)
        {
            try
            {

                bool removeTrailingDot = _configRoot.GetValue<bool>("RemoveTrailingDot");

                if (removeTrailingDot && name.EndsWith("."))
                {
                    name = name.Remove(name.Length - 1);
                }

                using (QueryResult queryResult = await _rqliteService.QueryAsync($"SELECT content FROM domains,domainmetadata WHERE domains.id=domainmetadata.domain_id AND LOWER(domains.name)='{name.ToLower()}' AND LOWER(domainmetadata.kind)='{kind.ToLower()}'"))
                {
                    return StringArrayResponse.FromValues(queryResult.Results.FirstOrDefault().Values);
                }

            }
            catch (NoValuesException)
            {
                _logger.LogDebug($"No metadata record '{kind}' found for domain '{name}'");
                return new BoolResponse { result = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to get DomainMetaData for domain '{name}' with kind '{kind}'" }, result = false };
            }
        }

        // GET PowerDNS/getdomainkeys/example.com/0
        [HttpGet("getdomainkeys/{name}/{kind}")]
        public IResponse getDomainKeys(string name, string kind)
        {
            return new BoolResponse { Log = new List<string>() { "Not implemented" }, result = false };
        }

        // GET PowerDNS/gettsigkey/example.com.
        [HttpGet("gettsigkey/{name}")]
        public IResponse getTSIGKey(string name)
        {
            return new BoolResponse { Log = new List<string>() { "Not implemented" }, result = false };
        }

        // GET PowerDNS/getdomaininfo/example.com.
        [HttpGet("getDomainInfo/{name}")]
        public async Task<IResponse> getDomainInfo(string name)
        {
            try
            {
                bool removeTrailingDot = _configRoot.GetValue<bool>("RemoveTrailingDot");

                if (removeTrailingDot && name.EndsWith("."))
                {
                    name = name.Remove(name.Length - 1);
                }


                using (QueryResult queryResult = await _rqliteService.QueryAsync($"SELECT id,name,master,last_check,type,notified_serial,account FROM domains WHERE LOWER(name)='{name.ToLower()}'"))
                {
                    return DomainInfoResponse.FromValues(queryResult.Results.FirstOrDefault().Values);
                }
            }
            catch (NoValuesException)
            {
                _logger.LogDebug($"No domaininfo records found for domain '{name}'");
                return new BoolResponse { result = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to get DomainInfo for domain '{name}'" }, result = false };
            }
        }

        // GET PowerDNS/isMaster/example.com/198.51.100.0.1
        [HttpGet("isMaster/{name}/{ip}")]
        public IResponse isMaster(string name, string ip)
        {
            return new BoolResponse { Log = new List<string>() { "Not implemented" }, result = false };
        }

       //GET PowerDNS/getAllDomains?includeDisabled = true
       [HttpGet("getAllDomains")]
        public async Task<IResponse> getAllDomains(bool includeDisabled)
        {
            try
            {
                using (QueryResult queryResult = await _rqliteService.QueryAsync($"SELECT id,name,master,last_check,type,notified_serial,account FROM domains"))
                {
                    return AllDomainsResponse.FromValues(queryResult.Results.FirstOrDefault().Values);
                }
            }
            catch (NoValuesException)
            {
                _logger.LogDebug($"No domain entries found in database");
                return new BoolResponse { result = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { "Failed to get all domains" }, result = false };
            }
        }

        // GET PowerDNS/searchRecords?q=www.example*&maxResults=100
        [HttpGet("searchRecords")]
        public IResponse searchRecords(string q, int maxResults)
        {
            return new BoolResponse { Log = new List<string>() { "Not implemented" }, result = false };
        }

        // GET PowerDNS/getUpdatedMasters
        [HttpGet("getUpdatedMasters")]
        public async Task<IResponse> getUpdatedMasters()
        {
            try
            {
                using (QueryResult queryResult = await _rqliteService.QueryAsync($"SELECT id,name,master,last_check,type,notified_serial,account FROM domains WHERE LOWER(kind)='master'"))
                {
                    return GetUpdatedMastersResponse.FromValues(queryResult.Results.FirstOrDefault().Values);
                }
            }
            catch (NoValuesException)
            {
                _logger.LogDebug($"No masters found");
                return new BoolResponse { result = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { "Failed to get updated Masters" }, result = false };
            }
        }

        // GET PowerDNS/getUnfreshSlaveInfos
        [HttpGet("getUnfreshSlaveInfos")]
        public IResponse getUnfreshSlaveInfos()
        {
            return new BoolResponse { Log = new List<string>() { "Not implemented" }, result = false };
        }


        // PATCH PowerDNS/setdomainmetadata/example.com/PRESIGNED
        [HttpPatch("setdomainmetadata/{name}/{kind}")]
        public async Task<IResponse> setDomainMetadata(string name, string kind, [FromBody] string[] value)
        {
            try
            {

                string query;

                int domain_id = await GetDomainID(name);

                if (value.Count() == 0)
                {
                    query = $"DELETE FROM domainmetadata WHERE domain_id={domain_id} AND LOWER(kind)='{kind.ToLower()}'";
                }
                else
                {
                    query = $"INSERT INTO domainmetadata (domain_id,kind,content) VALUES ({domain_id},'{kind}','{value[0]}')";
                }

                using (ExecuteResult execResult = await _rqliteService.ExecuteAsync(query))
                {
                    if (execResult.Results.FirstOrDefault().RowsAffected <= 0)
                    {
                        return new BoolResponse { result = false };
                    }
                    else
                    {
                        return new BoolResponse { result = true };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to update Domain meta data for domain {name} => Kind: {kind} | value: {value}" }, result = false };
            }
        }

        // PATCH PowerDNS/setnotified/1
        [HttpPatch("setnotified/{id}")]
        public async Task<IResponse> setNotified(int id, [FromForm] int serial)
        {
            try
            {
                using (ExecuteResult execResult = await _rqliteService.ExecuteAsync($"UPDATE domains SET notified_serial={serial} WHERE id={id}"))
                {
                    if (execResult.Results.FirstOrDefault().RowsAffected <= 0)
                    {
                        return new BoolResponse { result = false };
                    }
                    else
                    {
                        return new BoolResponse { result = true };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to update notified_serial for domain_id {id}" }, result = false };
            }
        }

        // PATCH PowerDNS/feedrecord/1370416133
        [HttpPatch("feedrecord/{trxid}")]
        public IResponse feedRecord(int trxid, [FromForm] Record rr)
        {
            try
            {
                int domain_id = _transactionManager.GetTransaction(trxid).domain_id;

                if (rr.domain_id <= 0) { rr.domain_id = domain_id; }

                if (_transactionManager.AddTransaction(trxid, GetInsertRecordQuery(rr),rr,Models.PowerDNS.Enums.TransactionMode.INSERT))
                {
                    return new BoolResponse { result = true };
                }
                else
                {
                    return new BoolResponse { result = false };
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to feed/Insert record {rr.qname}" }, result = false };
            }
        }

        // PATCH PowerDNS/feedrecord/
        [HttpPatch("feedrecord")]
        public async Task<IResponse> feedRecord([FromForm] Record rr)
        {

            try
            {
                bool removeTrailingDot = _configRoot.GetValue<bool>("RemoveTrailingDot");

                if (removeTrailingDot && rr.qname.EndsWith("."))
                {
                    rr.qname = rr.qname.Remove(rr.qname.Length - 1);
                }

                if (rr.domain_id <= 0)
                {
                    throw new Exception("DomainId must be set!");
                }

                return await InsertRecord(rr);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to feed/Insert record {rr.qname}" }, result = false };
            }
        }


        // PATCH PowerDNS/replacerrset/2/replace.example.com/A
        [HttpPatch("replacerrset/{domain_id}/{qname}/{qtype}")]
        public async Task<IResponse> replaceRRSet(int domain_id, string qname, string qtype, [FromForm] int trxid,[FromForm] Record[] rrset)
        {
            try
            {
                bool result = false;
                List<string> querys = new List<string>();
                bool removeTrailingDot = _configRoot.GetValue<bool>("RemoveTrailingDot");
                int transactionid = -1;
                

                if (removeTrailingDot && qname.EndsWith("."))
                {
                    qname = qname.Remove(qname.Length - 1);
                }

                if (_transactionManager.Transactions().Any(x => x.domain_id == domain_id)) { transactionid = _transactionManager.GetLastTransaction(domain_id).id; }

                if (rrset.Length > 1)
                {
                    rrset.SkipLast(1).AsParallel().ForAll(x => x.ttl = (x.ttl / 10));
                }

                if (transactionid != -1)
                {
                    if (!_transactionManager.AddTransaction(transactionid, GetDeleteRecordQuery(domain_id, qname, qtype), domain_id, qname, qtype, Models.PowerDNS.Enums.TransactionMode.DELETE))
                    {
                        throw new Exception($"failed to add transaction record to transaction with id {transactionid}");
                    }
                }
                else
                {
                    querys.Add(GetDeleteRecordQuery(domain_id, qname, qtype));
                }

                foreach (Record rr in rrset)
                {

                    if (removeTrailingDot && rr.qname.EndsWith("."))
                    {
                        rr.qname = rr.qname.Remove(rr.qname.Length - 1);
                    }

                    rr.domain_id = domain_id;

                    if (transactionid != -1)
                    {
                        if (!_transactionManager.AddTransaction(transactionid, GetInsertRecordQuery(rr), rr, Models.PowerDNS.Enums.TransactionMode.INSERT))
                        {
                            throw new Exception($"failed to add transaction record to transaction with id {transactionid}");
                        }
                    }
                    else
                    {
                        querys.Add(GetInsertRecordQuery(rr));
                    }

                }

                if (querys != null && querys.Count > 0)
                {

                    using (ExecuteResult execResult = await _rqliteService.ExecuteBulkAsync(querys))
                    {
                        if (execResult.Results.Any(x => x is null || x.LastInsertId <= 0))
                        {
                            throw new Exception("One or more transactions failed to execute!");
                        }
                    }
                }

                result = true;

                return new BoolResponse() { result = result };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to replace rrset for record '{qname}' for domain_id {domain_id}" }, result = false };
            }
        }


        // POST PowerDNSAPI/starttransaction/1/example.com
        [HttpPost("starttransaction/{domain_id}/{domain}/{trxid}")]
        public async Task <IResponse> startTransaction(int domain_id, string domain ,int trxid)
        {

            try
            {
                if (domain_id <= 0)
                {
                    domain_id = await GetDomainID(domain);
                }

                return new BoolResponse { result = _transactionManager.StartTransaction(trxid, domain_id, domain) };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to start transaction for domain {domain}" }, result = false };
            }
        }

        // POST PowerDNSAPI/committransaction/1
        [HttpPost("committransaction/{trxid}")]
        public async Task<IResponse> commitTransaction(int trxid)
        {
            try
            {
                var transactions = _transactionManager.GetTransactionQuerys(trxid);

                if (transactions != null && transactions.Count > 0)
                {

                    using (ExecuteResult execResult = await _rqliteService.ExecuteBulkAsync(transactions))
                    {
                        if (execResult.Results.Any(x => x is null || x.LastInsertId <= 0))
                        {
                            throw new Exception("One or more transactions failed to execute!");
                        }
                    }

                    return new BoolResponse { result = true };
                }
                else
                {
                    throw new Exception("transactions were null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to commit Transaction with id {trxid}" }, result = false };
            }
            finally
            {
                try
                {
                    _transactionManager.RemoveTransaction(trxid);
                }
                catch (Exception)
                {
                    _logger.LogError("Failed to remove/cleanup transaction");
                }
                
            }

        }

        // POST PowerDNSAPI/aborttransaction/1
        [HttpPost("aborttransaction/{trxid}")]
        public IResponse abortTransaction(int trxid)
        {
            try
            {
                return new BoolResponse { result = _transactionManager.RemoveTransaction(trxid) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { $"Failed to abort Transaction with id {trxid}" }, result = false };
            }
            
        }

        #region "Helper"

        private string GetInsertRecordQuery(Record rr)
        {
            return $"INSERT INTO records (domain_id,name,type,content,ttl,disabled,ordername,auth) VALUES ({rr.domain_id},'{rr.qname}','{rr.qtype}','{rr.content}',{rr.ttl},{rr.disabled},NULL,{rr.auth})";
        }

        //private string GetUpdateRecordQuery(Record rr, int domain_id, string qname, string qtype)
        //{
        //    return $"UPDATE records SET name='{rr.qname}',type='{rr.qtype}',content='{rr.content}',ttl={rr.ttl},disabled={rr.disabled},auth={rr.auth} WHERE domain_id={domain_id} AND (LOWER(name)='{qname.ToLower()}' AND type='{qtype}' AND content='{rr.content}')";
        //}

        private string GetDeleteRecordQuery(int domain_id, string qname, string qtype)
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

        //private async Task<bool> RecordExists(int domain_id,string qname,string qtype, string content = null)
        //{

        //    string query = string.IsNullOrWhiteSpace(content) ? $"SELECT name FROM records WHERE domain_id={domain_id} AND (LOWER(name)='{qname.ToLower()}' AND type='{qtype}')" : $"SELECT name FROM records WHERE domain_id={domain_id} AND (LOWER(name)='{qname.ToLower()}' AND type='{qtype}' AND content='{content}')";

        //    using (QueryResult queryResult = await _rqliteService.QueryAsync(query))
        //    {
        //        if (queryResult.Results.FirstOrDefault().Values is null || queryResult.Results.FirstOrDefault().Values.Count == 0)
        //        {
        //            return false;
        //        }
        //        else
        //        {
        //            return true;
        //        }
        //    }
        //}

        private async Task<int> GetDomainID(string domainname)
        {
            using (QueryResult queryResult = await _rqliteService.QueryAsync($"SELECT id FROM domains WHERE LOWER(name)='{domainname.ToLower()}'"))
            {

                if (queryResult.Results.FirstOrDefault().Values is null || queryResult.Results.FirstOrDefault().Values.Count <= 0)
                {
                    throw new Exception($"Failed to get id for domain name '{domainname}'");
                }
                else
                {
                    return int.Parse(queryResult.Results.FirstOrDefault().Values[0][0].ToString());
                }
            }
        }

        //private async Task<IResponse> DeleteRecord(int domain_id, string qname, string qtype)
        //{
        //    try
        //    {
        //        string query = GetDeleteRecordQuery(domain_id, qname, qtype);

        //        using (ExecuteResult execResult = await _rqliteService.ExecuteAsync(query))
        //        {

        //            if (execResult.Results.Any(x => x is null || x.LastInsertId <= 0))
        //            {
        //                return new BoolResponse { result = false };
        //            }
        //            else
        //            {
        //                return new BoolResponse { result = true };
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex.Message);
        //        return new BoolResponse { Log = new List<string>() { ex.Message }, result = false };
        //    }
        //}

        //private async Task<IResponse> UpdateRecord(Record rr, int domain_id, string qname, string qtype)
        //{
        //    try
        //    {

        //        string query = GetUpdateRecordQuery(rr, domain_id, qname, qtype);

        //        using (ExecuteResult execResult = await _rqliteService.ExecuteAsync(query))
        //        {

        //            if (execResult.Results.Any(x => x is null || x.LastInsertId <= 0))
        //            {
        //                return new BoolResponse { result = false };
        //            }
        //            else
        //            {
        //                return new BoolResponse { result = true };
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex.Message);
        //        return new BoolResponse { Log = new List<string>() { ex.Message }, result = false };
        //    }
        //}

        private async Task<IResponse> InsertRecord(Record rr)
        {
            try
            {
                string query = GetInsertRecordQuery(rr);

                using (ExecuteResult execResult = await _rqliteService.ExecuteAsync(query))
                {

                    if (execResult.Results.Any (x => x.RowsAffected <= 0))
                    {
                        return new BoolResponse { result = false };
                    }
                    else
                    {
                        return new BoolResponse { result = true };
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new BoolResponse { Log = new List<string>() { ex.Message }, result = false };
            }
        }

        #endregion
    }
}
