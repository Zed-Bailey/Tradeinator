using System.Globalization;
using Alpaca.Markets;
using CsvHelper;
using CsvHelper.Configuration;
using SimpleBacktestLib;
using Spectre.Console;

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
            using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
            {
                candleData = csv.GetRecords<BacktestCandle>().ToList();
            }
        }
        else
        {
            Console.WriteLine("No existing data path found, loading from api");
            candleData = (await FetchDataFromAlpaca(symbol, fromDate, toDate, timeFrame, dataClient)).ToList();
            if (candleData.Count > 0)
            {
                Console.WriteLine("Writing data to csv file");
                using (var writer = new StreamWriter(dataPath))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    await csv.WriteRecordsAsync(candleData);
                }    
            }
        }

        return candleData;
    }

    /// <summary>
    /// Fetch historical crypto data from alpaca
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="timeFrame"></param>
    /// <param name="dataClient"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<BacktestCandle>> FetchDataFromAlpaca(string symbol, DateTime from, DateTime to, BarTimeFrame timeFrame,  IAlpacaCryptoDataClient dataClient)
    {
        var bars = new List<IBar>();

        await AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),        // Progress bar
                new SpinnerColumn()
                
            })
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Downloading data[/]");
                task.IsIndeterminate = true;
                task.StartTask();
                try
                {
                    
                    var page = await dataClient.ListHistoricalBarsAsync(
                        new HistoricalCryptoBarsRequest(symbol, from, to, timeFrame));
                    
                    if (page.Items.Count == 0) throw new Exception("Response returned no data");
                     
                    
                    bars.AddRange(page.Items);
                    var paginationToken = page.NextPageToken;
                    task.Description = $"[green]Downloading data [/] | [blue]{paginationToken}[/]";
                    while (paginationToken != null)
                    {
                        
                        // Console.WriteLine($"Getting next page of data. token={paginationToken}");
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
                    
                    task.StopTask();
                }
                catch (Exception e)
                {
                    task.StopTask();
                    AnsiConsole.WriteException(e);
                }
            });
        
        Console.WriteLine($"loaded {bars.Count} bars");
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