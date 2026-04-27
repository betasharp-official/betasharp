#!/bin/sh
set -eu

copy_if_missing() {
  src="$1"
  dst="$2"

  if [ -f "$src" ] && [ ! -f "$dst" ]; then
    cp "$src" "$dst"
  fi
}

mkdir -p /data

copy_if_missing /seed-data/server.properties /data/server.properties
copy_if_missing /seed-data/b1.7.3.jar /data/b1.7.3.jar

if [ ! -f /data/b1.7.3.jar ]; then
  echo "Missing /data/b1.7.3.jar. Place b1.7.3.jar in BetaSharp.Ops/monitoring/server-data/ before building the image." >&2
  exit 1
fi

exec dotnet /app/BetaSharp.Server.dll
