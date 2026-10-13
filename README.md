![PowerRqlite](Resources/logo-text.png)

*PowerRqlite* is a small [PowerDNS Authoritative](https://powerdns.com) backend which allows to use a [rqlite](https://github.com/rqlite/rqlite) cluster as database / storage.

![GitHub](https://img.shields.io/github/license/KlettIT/PowerRqlite?style=plastic)


## How it works

*PowerRqlite* implements the required PowerDNS [remote backend functions](https://doc.powerdns.com/authoritative/backends/remote.html) and stores the data in the rqlite cluster using the [rqlite Data API](https://rqlite.io/docs/api/api/).

A simple deployment could be look like this:

![SimpleDeployment](docs/SimpleDeployment.png)

*PowerRqlite* can be also be used to build a High Availbility setup **without** need for (complex) HA Backend MariaDB Galera Cluster.

![HADeployment](docs/HADeployment.png)

## Prerequisites

* .NET 10 compatible Operating System (Ubuntu/Debian/CentOS/Alpine Linux/Windows/macOS) or a Docker Host
* [rqrlite Cluster](https://github.com/rqlite/rqlite/blob/master/DOC/CLUSTER_MGMT.md)
* PowerDNS Authoritative 4.3.x or higher, including 5.x (tested with 4.9 and 5.1; see Known Issues for 5.x)

*PowerRqlite* can be used for the following DNS Modes of Operation:

| Name        | Native | Master | Slave | SuperSlave | DNSSEC | Launch |
|-------------|:------:|:------:|:-----:|:----------:|:------:|:------:|
| PowerRqlite |   yes  |   yes  |   no  |     no     |   no   | remote |



## Known Issues

* `pdnsutil delete-zone` and the PowerDNS Web API cannot delete a zone: the PowerDNS
  remote backend module never sends a zone-deletion method (upstream gap).
  Delete a zone through `pdnsutil backend-cmd` or the PowerRqlite extension endpoint instead:
  ```
  pdnsutil backend-cmd remote "delete-zone example.com"
  curl -X POST http://127.0.0.1:5000/PowerDns/deleteDomain/example.com.
  ```
* The remote backend module has no zone-creation method either, so PowerRqlite creates a zone
  the first time PowerDNS asks for its domain info (that is what makes `pdnsutil create-zone` work on PowerDNS 4.x).
  Anything else that makes PowerDNS ask for an unknown zone creates an empty zone too, for example a
  DNS UPDATE from a client allowed by `allow-dnsupdate-from`. Keep that list (or TSIG) tight, or turn
  this off with `PowerRqlite:AutoCreateZones` (see [Configuration](#configuration)); on PowerDNS 5.x it is not needed.
* PowerDNS 5.x refuses `pdnsutil create-zone` (and `load-zone`, `create-secondary-zone`, `zone copy`) for the
  remote backend ("None of the configured backends support zone creation"). Create a zone with:
  ```
  pdnsutil backend-cmd remote "create-zone example.com ns1.example.com"
  ```
  This also completes a zone that a failed `pdnsutil create-zone` left behind without SOA.
* `pdnsutil` 5.x takes absolute record names: `pdnsutil add-record example.com www.example.com A 300 192.0.2.1`
  (4.x: `www`).
* Transactions (`pdnsutil` edits, DNS UPDATE, Web API changes) are held in the memory of the PowerRqlite instance
  that received `startTransaction`. Every PowerDNS server must therefore always talk to the same PowerRqlite
  instance, as in the deployments above (one PowerRqlite next to each PowerDNS). Do not put a round-robin load
  balancer between PowerDNS and several PowerRqlite instances.
* The remote backend never tells PowerRqlite whether disabled records are wanted. DNS lookups never return
  disabled records; zone listings (AXFR, `pdnsutil list-zone`, Web API) and Web API lookups include them.


## Quick Start

### rqlite

Grab the latest release from the rqlite [Github release page](https://github.com/rqlite/rqlite/releases). Once installed you can start a single rqlite node:

```
rqlited -node-id 1 ~/node.1
```

### PowerDNS

Install PowerDNS Authoritative a documented [here](https://doc.powerdns.com/authoritative/installation.html).
To use *PowerRqlite* you also have to install the `pdns-backend-remote` package.

Once installed we need to configure the remote-backend. To do this edit the `pdns.conf` configuration file add the following at the end of the file:

```
launch=remote
remote-connection-string=http:url=http://127.0.0.1:5000/PowerDNS,timeout=20000
```

### PowerRqlite

Grab the latest release from the rqlite [Github release page](https://github.com/KlettIT/PowerRqlite/releases). Once installed you can start it:

```
chmod +x PowerRqlite
./PowerRqlite
```

In case rqlite is not running on the same node, you can configure the rqlite url in the `appsettings.json` configuration file. Just edit `Url` under the `rqliteOptions` section.

When running as a Docker container you can configure it via the `rqliteOptions__Url` Environment variable.

## Configuration

*PowerRqlite* is ASP.NET Core application and will therefore configured via the file `appsettings.json`.
If you running *PowerRqlite* as a Docker container you can use Environment variables to configure *PowerRqlite*.

### Change default Port

*PowerRqlite* listens default on Port `5000`. In case you want to change it, you just need to edit `appsettings.json` configuration file. In this example we change the listen port to `5555`

```
{
...
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "http://localhost:5555"
      }
    }
  }
}
```

Translated into "Docker" it would be the following environment variable: `ASPNETCORE_Kestrel__EndPoints__Http__Url=http://localhost:5555`

### Automatic zone creation

By default *PowerRqlite* creates a zone the first time PowerDNS asks for an unknown one (needed for `pdnsutil create-zone` on PowerDNS 4.x).
To turn it off, for example on PowerDNS 5.x where zones are created with `pdnsutil backend-cmd remote "create-zone ..."`:

```
{
...
  "PowerRqlite": {
    "AutoCreateZones": false
  }
}
```

Docker: `PowerRqlite__AutoCreateZones=false`

### Health check

`GET /health` answers `Healthy` (HTTP 200) when rqlite can be queried, otherwise `Unhealthy` (HTTP 503).
Use it for load balancer, Docker or Kubernetes health checks.

## Tests

The tests need the .NET 10 SDK and a running Docker; rqlite, PowerDNS and the DNS tools are started with Testcontainers.

```
dotnet test tests/PowerRqlite.Tests
```

PowerDNS 5.1 is tested by default; `PDNS_IMAGE` selects another image (CI runs 4.9 and 5.1):

```
PDNS_IMAGE=powerdns/pdns-auth-49:4.9.17 dotnet test tests/PowerRqlite.Tests
```

The end-to-end tests drive a real PowerDNS with `pdnsutil`, `dig` and `nsupdate`. The same setup can be started by hand:

```
docker compose -f tests/docker/docker-compose.yml up --build
```
