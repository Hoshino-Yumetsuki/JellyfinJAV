# Changelog

## 2026.11.02.1

- Use the latest nonempty English machine translation when a movie has no original English title.
- Preserve original English titles, with Japanese titles as the fallback when no English translation is available.
- Preserve actress names during person metadata updates so movie cast links stay visible in Jellyfin.
- Include release notes in the plugin catalog and installed plugin metadata so Jellyfin can display the changelog.
- Keep the packaged plugin name consistent with Jellyfin's loaded plugin name and normalize its metadata version to prevent duplicate-version restart loops.

## 2026.11.02.0

- Add compatibility with Jellyfin 12 and .NET 10.
- Fix loading and saving plugin settings without jQuery.
- Update development and packaging setup for Jellyfin 12.
