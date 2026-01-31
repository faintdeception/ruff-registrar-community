#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
apphost_project="$repo_root/src/StudentRegistrar.AppHost/StudentRegistrar.AppHost.csproj"
out_dir="$repo_root/dist/aspire"
out_file="$out_dir/aspire-manifest.json"

mkdir -p "$out_dir"

# Generates an Aspire deployment manifest (JSON) from the AppHost.
# Docs: https://aspire.dev/deployment/manifest-format/
aspire do publish-manifest \
  --project "$apphost_project" \
  --output-path "$out_file" \
  --non-interactive

echo "Wrote: $out_file"
