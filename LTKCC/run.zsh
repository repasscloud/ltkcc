#!/usr/bin/env zsh

rm -rf bin obj
dotnet run -f net8.0-maccatalyst --no-launch-profile

