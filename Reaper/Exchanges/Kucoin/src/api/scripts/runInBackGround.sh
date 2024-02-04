#!/bin/bash
# run dotnet in background

nohup dotnet run --project ~/projects/reaper/Reaper/Exchanges/Kucoin/src/api/Reaper.Exchanges.Kucoin.Api.csproj &> ~/projects/reaper/Reaper/Exchanges/Kucoin/src/api/kucoin.log &

