# JellyfinJav
Don't expect perfection.

This branch targets Jellyfin **12.x** using the **12.0.0** plugin APIs and
**.NET 10**. Jellyfin 12 requires plugins to be retargeted and rebuilt; see the
[official release notes](https://github.com/jellyfin/jellyfin/releases/tag/v12.0).
Older Jellyfin 10.11 builds remain in the catalog for older servers.

# Metadata Providers
* R18.dev (Videos)
* AsianScreens (Actresses)
* Warashi Asian Pornstars (Actresses)

# Instructions
### Installation

Add the Repo url to Jellyfin using the `manifest.json` asset from the latest GitHub release:
`https://github.com/gooner-dev99/JellyfinJAV/releases/latest/download/manifest.json`


### Usage
When adding the media library, make sure to select "Content type: movies".

Movie titles prefer the original English title, then the latest nonempty English
machine translation from the imported dump, then the Japanese title. Refresh
existing movie metadata to apply title updates.

Actresses appear in Jellyfin's cast/people list as Actors; there is no separate
Actress metadata field. The plugin settings control their name order.

### Example File Names
* abp200.mkv
* ABP200.mkv
* ABP-200.mkv
* some random text abp-200 more random text.mkv
> This should still be how it works, I didn't change any of the filename detection, I've only ever tested with ABP-200 type of file naming.

# Development
### Requirements
* Docker
* Docker Compose
* Python
* .NET 10.0 SDK
* Node.js 24 (for settings-page tests)

### Building

To build without starting Docker:

    $ dotnet restore JellyfinJav.sln
    $ dotnet build JellyfinJav.sln --configuration Release --no-restore

To build and start the development server (the Compose file pins Jellyfin 12.0):

    $ ./build.sh
    # Visit localhost:8896

### Packaging
    $ python package.py
    # manifest.json will update, and the package will be zipped up in release/

Packaging derives ABI `12.0.0.0` from the Jellyfin.Controller reference and adds
a new catalog entry with the ZIP's checksum, preserving existing releases.
It requires a successful Release build and copies AngleSharp and Npgsql from
`NUGET_PACKAGES` (or `~/.nuget/packages` by default). Upload the ZIP and generated
`release/manifest.json` together to the matching GitHub release.

The checked-in catalog includes release `2026.11.02.1` for Jellyfin 12, while
preserving previous releases for older servers.

Add release notes under the matching version heading in `CHANGELOG.md` before
packaging. The notes are included in the catalog and the ZIP's `meta.json` for
Jellyfin's plugin page.

### Validation

    $ node --test Tests/config_page.test.cjs
    $ dotnet test Tests/Tests.csproj --configuration Release --no-restore

The settings-page tests run without Jellyfin or a database. The existing R18
tests require a populated PostgreSQL database and contain legacy fixture
assertions; they do not provide an isolated compatibility check. Before
publishing, also verify plugin loading, saving settings, movie identification
and refresh, and movie/person artwork on Jellyfin 12 with a configured database.

### General
JAV files for testing purposes are stored in videos/

### R18 Database Environment Variables
Set these on the Jellyfin container if you want the plugin to query the PostgreSQL database that contains the imported R18 dump.

* `JELLYFINJAV_R18_DB_CONNECTION_STRING`
* `JELLYFINJAV_R18_DB_HOST`
* `JELLYFINJAV_R18_DB_PORT`
* `JELLYFINJAV_R18_DB_NAME`
* `JELLYFINJAV_R18_DB_USER`
* `JELLYFINJAV_R18_DB_PASSWORD`
* `JELLYFINJAV_R18_DB_SSL_MODE`

If `JELLYFINJAV_R18_DB_CONNECTION_STRING` is not set, the plugin builds a connection string from host, port, database, user, and password. If none of the database environment variables are set, the R18 lookup providers return unavailable results.

# Screenshots
![Grid Example](screenshots/example-grid.jpg)
![Video Example](screenshots/example-video.jpg)
![Actress Example](screenshots/example-actress.jpg)

# License
Licensed under AGPL-3.0-only
