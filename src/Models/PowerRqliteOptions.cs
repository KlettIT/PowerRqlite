namespace PowerRqlite.Models
{
    /// <summary>
    /// Settings of the "PowerRqlite" configuration section (environment: PowerRqlite__AutoCreateZones=false).
    /// </summary>
    public class PowerRqliteOptions
    {
        /// <summary>
        /// Create a zone the first time PowerDNS asks for its domain info. PowerDNS 4.x needs this for
        /// 'pdnsutil create-zone'; on 5.x zones are created with 'pdnsutil backend-cmd remote "create-zone ..."'.
        /// When on, a DNS UPDATE for an unknown zone creates an empty zone too.
        /// </summary>
        public bool AutoCreateZones { get; set; } = true;
    }
}
