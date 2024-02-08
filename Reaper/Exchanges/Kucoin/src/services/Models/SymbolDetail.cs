namespace Reaper.Exchanges.Kucoin.Services;

public class SymbolDetail
{
    public string Code { get; set; } = string.Empty;
    public SymbolData Data { get; set; } = new();
}

public class SymbolData 
{

    public string Symbol { get; set; } = string.Empty;

    public decimal MarkPrice { get; set; }

    public decimal MakerFeeRate { get; set; }

    public decimal TakerFeeRate { get; set; }

    public decimal HighPrice { get; set; }

    public decimal LowPrice { get; set; }

    public decimal PriceChg { get; set; }

    public decimal PriceChgPct { get; set; }
}