#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

dotnet test tests/Unit/Unit.csproj -m:1 --disable-build-servers "$@"
dotnet test tests/Integration/Integration.csproj -m:1 --disable-build-servers "$@"
