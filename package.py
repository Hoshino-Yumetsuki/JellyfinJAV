#!/usr/bin/env python
import xml.etree.ElementTree as ET
from datetime import datetime
from pathlib import Path
from hashlib import md5
import json
import re
import subprocess
import shutil

def detect_github_repo() -> str:
    repo_name = Path.cwd().name
    owner_from_path = None
    path_parts = Path.cwd().parts
    if "contributions" in path_parts:
        idx = path_parts.index("contributions")
        if idx + 2 < len(path_parts):
            owner_from_path = path_parts[idx + 1]

    repo_from_path = f"{owner_from_path}/{repo_name}" if owner_from_path else None

    try:
        remote = subprocess.check_output(
            ["git", "config", "--get", "remote.origin.url"],
            text=True,
        ).strip()
    except Exception:
        remote = ""

    match = re.search(r"github\.com[:/](?P<owner>[^/]+)/(?P<repo>[^/.]+)", remote)
    if match:
        return f"{match.group('owner')}/{match.group('repo')}"

    github_repository = os.environ.get("GITHUB_REPOSITORY")
    if github_repository:
        return github_repository

    return repo_from_path or f"{repo_name}/{repo_name}"


def normalize_abi_version(version: str) -> str:
    cleaned = re.sub(r"-\w+$", "", version)
    if cleaned.count(".") == 2:
        return f"{cleaned}.0"

    return cleaned


def read_changelog(version: str) -> str:
    contents = Path("CHANGELOG.md").read_text(encoding="utf-8")
    match = re.search(
        rf"^## {re.escape(version)}\s*\n(.*?)(?=^## |\Z)",
        contents,
        flags=re.MULTILINE | re.DOTALL,
    )
    if not match or not match.group(1).strip():
        raise ValueError(f"Missing changelog for release {version}")
    return match.group(1).strip()


import os

tree = ET.parse("JellyfinJav/JellyfinJav.csproj")
version = tree.find("./PropertyGroup/AssemblyVersion").text
changelog = read_changelog(version)
metadata_version = ".".join(str(int(part)) for part in version.split("."))
target_framework = tree.find("./PropertyGroup/TargetFramework").text
target_abi_raw = tree.find("./ItemGroup/*[@Include='Jellyfin.Controller']").attrib["Version"]
target_abi = normalize_abi_version(target_abi_raw)
anglesharp_version = tree.find("./ItemGroup/*[@Include='AngleSharp']").attrib["Version"]
timestamp = datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")
repo_slug = detect_github_repo()
repo_owner = repo_slug.split("/")[0]

meta = {
    "category": "Metadata",
    "guid": "1d5fffc2-1028-4553-9660-bd4966899e44",
    "name": "Jellyfin JAV",
    "changelog": changelog,
    "description": "JAV metadata providers for Jellyfin.",
    "owner": repo_owner,
    "overview": "JAV metadata providers for Jellyfin.",
    "targetAbi": target_abi,
    "timestamp": timestamp,
    "version": metadata_version
}

Path(f"release/{version}").mkdir(parents=True, exist_ok=True)
plugin_dir = Path(f"release/JellyfinJav_{version}")
if plugin_dir.exists():
    shutil.rmtree(plugin_dir)
plugin_dir.mkdir(parents=True, exist_ok=True)

print(json.dumps(meta, indent=4), file=open(plugin_dir / "meta.json", "w"))

subprocess.run([
    "dotnet",
    "build",
    "JellyfinJav/JellyfinJav.csproj",
    "--configuration",
    "Release"
], check=True)

build_output_dir = Path(f"JellyfinJav/bin/Release/{target_framework}")
for file_path in build_output_dir.glob("*"):
    if file_path.is_file():
        shutil.copy(file_path, plugin_dir / file_path.name)

nuget_packages = Path(os.environ.get("NUGET_PACKAGES", Path.home() / ".nuget/packages"))
shutil.copy(nuget_packages / f"anglesharp/{anglesharp_version}/lib/netstandard2.0/AngleSharp.dll", plugin_dir)

shutil.make_archive(f"release/jellyfinjav_{version}", "zip", plugin_dir)

entry = {
    "checksum": md5(open(f"release/jellyfinjav_{version}.zip", "rb").read()).hexdigest(),
    "changelog": changelog,
    "targetAbi": target_abi,
    "sourceUrl": f"https://github.com/{repo_slug}/releases/download/v{version}/jellyfinjav_{version}.zip",
    "timestamp": timestamp,
    "version": version
}

manifest = json.loads(open("manifest.json", "r").read())

manifest[0]["owner"] = repo_owner
manifest[0]["name"] = meta["name"]

manifest[0]["versions"] = [v for v in manifest[0]["versions"] if v.get("version") != version]

manifest[0]["versions"].insert(0, entry)
print(json.dumps(manifest, indent=4), file=open("manifest.json", "w"))
shutil.copy("manifest.json", "release/manifest.json")
