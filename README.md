![PowerRqlite](Resources/logo-text.png)

*PowerRqlite* is a small [PowerDNS Authoritative](https://powerdns.com) backend which allows to use a [rqlite](https://github.com/rqlite/rqlite) cluster as database / storage.

![GitHub](https://img.shields.io/github/license/KlettIT/PowerRqlite?style=plastic)


## How it works

*PowerRqlite* implements the required PowerDNS [remote backend functions](https://doc.powerdns.com/authoritative/backends/remote.html) and stores the data in the rqlite cluster using the [rqlite Data API](https://github.com/rqlite/rqlite/blob/master/DOC/DATA_API.md).

A simple deployment could be look like this:

![SimpleDeployment](docs/SimpleDeployment.png)

*PowerRqlite* can be also be used to build a High Availbility setup **without** need for (complex) HA Backend MariaDB Galera Cluster.

![HADeployment](docs/HADeployment.png)

## Prerequisites

* .NET 10 compatible Operating System (Ubuntu/Debian/CentOS/Alpine Linux/Windows/macOS) or a Docker Host
* [rqrlite Cluster](https://github.com/rqlite/rqlite/blob/master/DOC/CLUSTER_MGMT.md)
* PowerDNS Authoritative 4.3.x or higher

*PowerRqlite* can be used for the following DNS Modes of Operation:

| Name        | Native | Master | Slave | SuperSlave | DNSSEC | Launch |
|-------------|:------:|:------:|:-----:|:----------:|:------:|:------:|
| PowerRqlite |   yes  |   yes  |   no  |     no     |   no   | remote |



## Known Issues

* Domains/Zones can not be deleted via pdnsutil. This have to be done via sql.
  ```
  DELETE FROM domain WHERE name='example.com.';
  ```


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
