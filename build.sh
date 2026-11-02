#!/bin/sh
set -e

(cd JellyfinJav; dotnet build)
mkdir -p ./config/plugins/JellyfinJav
docker compose kill
docker compose up -d
