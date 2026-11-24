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

The plugin fetches movie metadata directly from R18.dev using the DVD code, not the filename or path. Titles use the site's English title when available, otherwise the Japanese title. No database or dump is required.

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
It requires a successful Release build and copies AngleSharp from
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

The settings-page and R18 web-client tests run offline. To also verify `BBAN-613`
against the live site, run `dotnet test Tests/Tests.csproj --filter Name=LiveBbanLookup`.
Before publishing, also verify plugin loading, saving settings, movie identification
and refresh, and movie/person artwork on Jellyfin 12.

### General
JAV files for testing purposes are stored in videos/

# Screenshots
![Grid Example](screenshots/example-grid.jpg)
![Video Example](screenshots/example-video.jpg)
![Actress Example](screenshots/example-actress.jpg)

# License
Licensed under AGPL-3.0-only
