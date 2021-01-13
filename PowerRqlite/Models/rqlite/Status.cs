using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PowerRqlite.Models.rqlite
{
    public partial class Status
    {
        [JsonProperty("build", NullValueHandling = NullValueHandling.Ignore)]
        public Build Build { get; set; }

        [JsonProperty("http", NullValueHandling = NullValueHandling.Ignore)]
        public Http Http { get; set; }

        [JsonProperty("node", NullValueHandling = NullValueHandling.Ignore)]
        public StatusNode Node { get; set; }

        [JsonProperty("runtime", NullValueHandling = NullValueHandling.Ignore)]
        public Runtime Runtime { get; set; }

        [JsonProperty("store", NullValueHandling = NullValueHandling.Ignore)]
        public Store Store { get; set; }
    }

    public partial class Build
    {
        [JsonProperty("branch", NullValueHandling = NullValueHandling.Ignore)]
        public string Branch { get; set; }

        [JsonProperty("build_time", NullValueHandling = NullValueHandling.Ignore)]
        public string BuildTime { get; set; }

        [JsonProperty("commit", NullValueHandling = NullValueHandling.Ignore)]
        public string Commit { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? Version { get; set; }
    }

    public partial class Http
    {
        [JsonProperty("addr", NullValueHandling = NullValueHandling.Ignore)]
        public string Addr { get; set; }

        [JsonProperty("auth", NullValueHandling = NullValueHandling.Ignore)]
        public string Auth { get; set; }

        [JsonProperty("redirect", NullValueHandling = NullValueHandling.Ignore)]
        public string Redirect { get; set; }
    }

    public partial class StatusNode
    {
        [JsonProperty("start_time", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? StartTime { get; set; }

        [JsonProperty("uptime", NullValueHandling = NullValueHandling.Ignore)]
        public string Uptime { get; set; }
    }

    public partial class Runtime
    {
        [JsonProperty("GOARCH", NullValueHandling = NullValueHandling.Ignore)]
        public string Goarch { get; set; }

        [JsonProperty("GOMAXPROCS", NullValueHandling = NullValueHandling.Ignore)]
        public long? Gomaxprocs { get; set; }

        [JsonProperty("GOOS", NullValueHandling = NullValueHandling.Ignore)]
        public string Goos { get; set; }

        [JsonProperty("num_cpu", NullValueHandling = NullValueHandling.Ignore)]
        public long? NumCpu { get; set; }

        [JsonProperty("num_goroutine", NullValueHandling = NullValueHandling.Ignore)]
        public long? NumGoroutine { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }
    }

    public partial class Store
    {
        [JsonProperty("addr", NullValueHandling = NullValueHandling.Ignore)]
        public string Addr { get; set; }

        [JsonProperty("apply_timeout", NullValueHandling = NullValueHandling.Ignore)]
        public string ApplyTimeout { get; set; }

        [JsonProperty("db_conf", NullValueHandling = NullValueHandling.Ignore)]
        public DbConf DbConf { get; set; }

        [JsonProperty("dir", NullValueHandling = NullValueHandling.Ignore)]
        public string Dir { get; set; }

        [JsonProperty("election_timeout", NullValueHandling = NullValueHandling.Ignore)]
        public string ElectionTimeout { get; set; }

        [JsonProperty("heartbeat_timeout", NullValueHandling = NullValueHandling.Ignore)]
        public string HeartbeatTimeout { get; set; }

        [JsonProperty("leader", NullValueHandling = NullValueHandling.Ignore)]
        public Leader Leader { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Metadatum> Metadata { get; set; }

        [JsonProperty("node_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? NodeId { get; set; }

        [JsonProperty("nodes", NullValueHandling = NullValueHandling.Ignore)]
        public List<NodeElement> Nodes { get; set; }

        [JsonProperty("raft", NullValueHandling = NullValueHandling.Ignore)]
        public Raft Raft { get; set; }

        [JsonProperty("request_marshaler", NullValueHandling = NullValueHandling.Ignore)]
        public RequestMarshaler RequestMarshaler { get; set; }

        [JsonProperty("snapshot_interval", NullValueHandling = NullValueHandling.Ignore)]
        public long? SnapshotInterval { get; set; }

        [JsonProperty("snapshot_threshold", NullValueHandling = NullValueHandling.Ignore)]
        public long? SnapshotThreshold { get; set; }

        [JsonProperty("sqlite3", NullValueHandling = NullValueHandling.Ignore)]
        public Sqlite3 Sqlite3 { get; set; }
    }

    public partial class DbConf
    {
        [JsonProperty("DSN", NullValueHandling = NullValueHandling.Ignore)]
        public string Dsn { get; set; }

        [JsonProperty("Memory", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Memory { get; set; }
    }

    public partial class Leader
    {
        [JsonProperty("addr", NullValueHandling = NullValueHandling.Ignore)]
        public string Addr { get; set; }

        [JsonProperty("node_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? NodeId { get; set; }
    }

    public partial class Metadatum
    {
        [JsonProperty("api_addr", NullValueHandling = NullValueHandling.Ignore)]
        public string ApiAddr { get; set; }

        [JsonProperty("api_proto", NullValueHandling = NullValueHandling.Ignore)]
        public string ApiProto { get; set; }
    }

    public partial class NodeElement
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? Id { get; set; }

        [JsonProperty("addr", NullValueHandling = NullValueHandling.Ignore)]
        public string Addr { get; set; }
    }

    public partial class Raft
    {
        [JsonProperty("applied_index", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? AppliedIndex { get; set; }

        [JsonProperty("commit_index", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? CommitIndex { get; set; }

        [JsonProperty("fsm_pending", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? FsmPending { get; set; }

        [JsonProperty("last_contact", NullValueHandling = NullValueHandling.Ignore)]
        public string LastContact { get; set; }

        [JsonProperty("last_log_index", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? LastLogIndex { get; set; }

        [JsonProperty("last_log_term", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? LastLogTerm { get; set; }

        [JsonProperty("last_snapshot_index", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? LastSnapshotIndex { get; set; }

        [JsonProperty("last_snapshot_term", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? LastSnapshotTerm { get; set; }

        [JsonProperty("latest_configuration", NullValueHandling = NullValueHandling.Ignore)]
        public string LatestConfiguration { get; set; }

        [JsonProperty("latest_configuration_index", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? LatestConfigurationIndex { get; set; }

        [JsonProperty("log_size", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? LogSize { get; set; }

        [JsonProperty("num_peers", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? NumPeers { get; set; }

        [JsonProperty("protocol_version", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? ProtocolVersion { get; set; }

        [JsonProperty("protocol_version_max", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? ProtocolVersionMax { get; set; }

        [JsonProperty("protocol_version_min", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? ProtocolVersionMin { get; set; }

        [JsonProperty("snapshot_version_max", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? SnapshotVersionMax { get; set; }

        [JsonProperty("snapshot_version_min", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? SnapshotVersionMin { get; set; }

        [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
        public string State { get; set; }

        [JsonProperty("term", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? Term { get; set; }
    }

    public partial class RequestMarshaler
    {
        [JsonProperty("compression_batch", NullValueHandling = NullValueHandling.Ignore)]
        public long? CompressionBatch { get; set; }

        [JsonProperty("compression_size", NullValueHandling = NullValueHandling.Ignore)]
        public long? CompressionSize { get; set; }

        [JsonProperty("force_compression", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ForceCompression { get; set; }
    }

    public partial class Sqlite3
    {
        [JsonProperty("db_size", NullValueHandling = NullValueHandling.Ignore)]
        public long? DbSize { get; set; }

        [JsonProperty("dsn", NullValueHandling = NullValueHandling.Ignore)]
        public string Dsn { get; set; }

        [JsonProperty("fk_constraints", NullValueHandling = NullValueHandling.Ignore)]
        public string FkConstraints { get; set; }

        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }
    }

    public partial class Status
    {
        public static Status FromJson(string json) => JsonConvert.DeserializeObject<Status>(json, PowerRqlite.JSON.Converter.Settings);
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }
}


