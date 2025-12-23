#!/usr/bin/env zsh
set -euo pipefail

killall -HUP -q LTKCC || true

rm -rf bin obj

dotnet run -f net8.0-maccatalyst --no-launch-profile
