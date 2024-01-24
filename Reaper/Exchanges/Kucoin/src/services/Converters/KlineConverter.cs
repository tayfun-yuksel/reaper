
// [
//   [
//     "1545904980", //Start time of the candle cycle
//     "0.058", //opening price
//     "0.049", //closing price
//     "0.058", //highest price
//     "0.049", //lowest price
//     "0.018", //Transaction volume
//     "0.000945" //Transaction amount
//   ],
//   ["1545904920", "0.058", "0.072", "0.072", "0.058", "0.103", "0.006986"]
// ]
using System.Text.Json;
using Reaper.Exchanges.Kucoin.Services.Models;

namespace Reaper.Exchanges.Kucoin.Services.Converters;
public static class KlineConverter
{
    public static Result<SpotKline[]> ToSpotKlineArray(this string jsonArray)
    {
        try
        {
            JsonDocument doc = JsonDocument.Parse(jsonArray);
            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("data", out JsonElement dataArray))
            {
                throw new InvalidOperationException("The JSON does not contain a 'data' property.");
            }

            List<SpotKline> marketDataList = [];
            foreach (JsonElement element in dataArray.EnumerateArray())
            {
                SpotKline marketData = new()
                {
                    OpenTime = element[0].GetString(),
                    Open = element[1].GetString(),
                    Close = element[2].GetString(),
                    High = element[3].GetString(),
                    Low = element[4].GetString(),
                    Volume = element[5].GetString(),
                    TransactionAmount = element[6].GetString(),
                };
                marketDataList.Add(marketData);
            }

        return new Result<SpotKline[]>{ Data = [.. marketDataList] };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing JSON: {ex.Message}");
            return new Result<SpotKline[]>{ Error = ex };
        }
    }

    public static Result<List<FuturesKline>> ToFuturesKlineArray(this string jsonArray)
    {
        try
        {
            JsonDocument doc = JsonDocument.Parse(jsonArray);
            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("data", out JsonElement dataArray))
            {
                throw new InvalidOperationException("The JSON does not contain a 'data' property.");
            }

            List<FuturesKline> marketDataList = [];
            foreach (JsonElement element in dataArray.EnumerateArray())
            {
                FuturesKline marketData = new()
                {
                    Time = element[0].GetInt64(),
                    EntryPrice = element[1].GetDecimal(),
                    HighestPrice = element[2].GetDecimal(),
                    LowestPrice = element[3].GetDecimal(),
                    ClosePrice = element[4].GetDecimal(),
                    TradingVolume = element[5].GetDecimal()
                };
                marketDataList.Add(marketData);
            }

            return new Result<List<FuturesKline>>{ Data = [.. marketDataList] };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing JSON: {ex.Message}");
            return new Result<List<FuturesKline>>{ Error = ex };
        }
    }
}