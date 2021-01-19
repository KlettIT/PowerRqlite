![PowerRqlite](PowerRqlite/Resources/logo-text.png)

*PowerRqlite* is a small [PowerDNS Authoritative](https://powerdns.com) backend which allows to use a [rqlite](https://github.com/rqlite/rqlite) cluster as database / storage.

## How it works

PowerRqlite implements the required PowerDNS [remote backend functions](https://doc.powerdns.com/authoritative/backends/remote.html) and stores the data in the rqlite cluster using the [rqlite Data API](https://github.com/rqlite/rqlite/blob/master/DOC/DATA_API.md).

A simple deployment could be look like this:

![SimpleDeployment](docs/SimpleDeployment.png)

PowerRqlite can be also be used to build a High Availbility setup **without** need for (complex) HA Backend MariaDB Galera Cluster.

![HADeployment](docs/HADeployment.png)

## Prerequisites

* .NET Core 3.1 compatible Operating System (Ubuntu/Debian/CentOS/Alpine Linux/Windows/macOS)
* [rqrlite Cluster](https://github.com/rqlite/rqlite/blob/master/DOC/CLUSTER_MGMT.md)
* PowerDNS Authoritative 4.3.x or higher

*PowerRqlite* can be used for the following DNS Modes of Operation:

| Name        | Native | Master | Slave | SuperSlave | DNSSEC | Launch |
|-------------|:------:|:------:|:-----:|:----------:|:------:|:------:|
| PowerRqlite |   yes  |   yes  |   no  |     no     |   no   | remote |


## Quick Start
