using System.Globalization;
using Alpaca.Markets;
using CsvHelper;
using SimpleBacktestLib;

namespace Tradeinator.Backtester.Helpers;

public static class DataFetcher
{
    
    public static async Task<IEnumerable<BacktestCandle>> GetData(string symbol, string localDataDirectoryPath, DateTime fromDate, DateTime toDate, BarTimeFrame timeFrame, IAlpacaCryptoDataClient dataClient)
    {
        var dataPath = Path.Combine(localDataDirectoryPath, $"{symbol.Replace("/", "_")}.csv");
        List<BacktestCandle> candleData;
        if (File.Exists(dataPath))
        {
            Console.WriteLine("existing data path found, loading from csv file");
            using (var reader = new StreamReader(dataPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                candleData = csv.GetRecords<BacktestCandle>().ToList();
            }
        }
        else
        {
            Console.WriteLine("No existing data path found, loading from api");
            candleData = (await FetchDataFromAlpaca(symbol, fromDate, toDate, timeFrame, dataClient)).ToList();
            Console.WriteLine("Writing data to csv file");
            using (var writer = new StreamWriter(dataPath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(candleData);
            }
        }

        return candleData;
    }

    // fetches historical data from alpaca
    public static async Task<IEnumerable<BacktestCandle>> FetchDataFromAlpaca(string symbol, DateTime from, DateTime to, BarTimeFrame timeFrame,  IAlpacaCryptoDataClient dataClient)
    {
        var page = await dataClient.ListHistoricalBarsAsync(
            new HistoricalCryptoBarsRequest(symbol, from, to, timeFrame));

        var bars = new List<IBar>(page.Items);
        var paginationToken = page.NextPageToken;
        while (paginationToken != null)
        {
            Console.WriteLine($"Getting next page of data. token={paginationToken}");
            var request = new HistoricalCryptoBarsRequest(symbol, from, to, timeFrame)
            {
                Pagination =
                {
                    Token = paginationToken
                }
            };
            page = await dataClient.ListHistoricalBarsAsync(request);
            paginationToken = page.NextPageToken;
            bars.AddRange(page.Items);
        }

        return bars.Select(x => new BacktestCandle
        {
            Open = x.Open,
            High = x.High,
            Low = x.Low,
            Close = x.Close,
            Time = x.TimeUtc,
            Volume = x.Volume
        });
    }

}