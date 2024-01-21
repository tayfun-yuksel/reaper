using System.Text.Json;
using Reaper.Exchanges.Binance.Services.ApiModels;

namespace Reaper.Exchanges.Binance.Services.Converters;
public static class KlineConverter
{
    public static Result<Kline[]> ToKlineArray(this string jsonArray)
    {
        try
        {
            JsonDocument doc = JsonDocument.Parse(jsonArray);
            JsonElement root = doc.RootElement;

            List<Kline> marketDataList = [];

            foreach (JsonElement element in root.EnumerateArray())
            {
                Kline marketData = new()
                {
                    OpenTime = element[0].GetUInt64(),
                    Open = element[1].GetString(),
                    High = element[2].GetString(),
                    Low = element[3].GetString(),
                    Close = element[4].GetString(),
                    Volume = element[5].GetString(),
                    CloseTime = element[6].GetUInt64(),
                    BaseAssetVolume = element[7].GetString(),
                    NumberOfTrades = element[8].GetInt32(),
                    TakerBuyVolume = element[9].GetString(),
                    TakerBuyBaseAssetVolume = element[10].GetString(),
                    // Ignore field is not included
                };

                marketDataList.Add(marketData);
            }

        return new Result<Kline[]>{ Data = [.. marketDataList] };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing JSON: {ex.Message}");
            return new Result<Kline[]>{ Error = ex };
        }
    }
}