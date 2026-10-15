#!/usr/bin/env bash
set -euo pipefail

release_sha="${GITHUB_SHA:?GITHUB_SHA no disponible}"
if [[ ! "$release_sha" =~ ^[0-9a-f]{40}$ ]]; then
  echo "SHA de publicación inválido" >&2
  exit 1
fi

build_dir="${GITHUB_WORKSPACE:?GITHUB_WORKSPACE no disponible}/UnityRoyalMatch/Builds/RoyalMatch_Web"
test -s "$build_dir/index.html"
test -s "$build_dir/Build/RoyalMatch_Web.wasm.gz"

python3 - "$build_dir/index.html" "$release_sha" <<'PY'
from pathlib import Path
import sys

index = Path(sys.argv[1])
release_sha = sys.argv[2]
html = index.read_text(encoding="utf-8")
placeholder = '__RELEASE_SHA__'
if html.count(placeholder) != 1:
    raise SystemExit("El template WebGL debe incluir un único marcador de versión")
index.write_text(html.replace(placeholder, release_sha), encoding="utf-8")
PY

release_base=/var/www/royal-match/releases
release_dir="$release_base/$release_sha"
site_root=/var/www/royal-match/royal_match
printf '{"commit":"%s"}\n' "$release_sha" > "$build_dir/build-info.json"

ssh -o BatchMode=yes vps "install -d -m 755 '$release_dir'"
rsync -a --delete "$build_dir/" "vps:$release_dir/"

ssh -o BatchMode=yes vps bash -s -- "$release_dir" "$site_root" "$release_sha" <<'REMOTE'
set -euo pipefail
release_dir="$1"
site_root="$2"
release_sha="$3"
test -s "$release_dir/index.html"
test -s "$release_dir/Build/RoyalMatch_Web.wasm.gz"
grep -Fq "$release_sha" "$release_dir/build-info.json"
grep -Fq "releaseVersion = \"$release_sha\"" "$release_dir/index.html"

current=""
if [ -L "$site_root/dist" ]; then
  current="$(readlink -f "$site_root/dist")"
elif [ -d "$site_root/dist" ]; then
  current="/var/www/royal-match/releases/legacy-$(date -u +%Y%m%d%H%M%S)"
  mv "$site_root/dist" "$current"
fi

ln -s "$release_dir" "$site_root/.dist-next"
mv -Tf "$site_root/.dist-next" "$site_root/dist"

served="$(curl --fail --silent --show-error --retry 4 --retry-delay 2 \
  --max-time 15 'https://olcas.app/royal/build-info.json' || true)"
if [ "$served" != "{\"commit\":\"$release_sha\"}" ]; then
  if [ -n "$current" ]; then
    ln -s "$current" "$site_root/.dist-rollback"
    mv -Tf "$site_root/.dist-rollback" "$site_root/dist"
  fi
  echo "La publicación no se ve en /royal/; se restauró la versión anterior" >&2
  exit 1
fi
echo "Publicado $release_sha en https://olcas.app/royal/"
REMOTE
