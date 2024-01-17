using Microsoft.Extensions.Logging;
using SimpleBacktestLib;
using Spectre.Console;

namespace Tradeinator.Backtester.Helpers;

public class FancyBackTest
{
    public static async Task<BacktestResult> RunFancyBacktest(BacktestBuilder builder, BacktestRunner backtest, int lastCandleIndex)
    {
        BacktestResult backtestResult = new BacktestResult();
        
        var logTable = new Table().Expand();
        logTable.AddColumn("Log Level");
        logTable.AddColumn("Message");


        var detailsTable = new Table().Expand();
        detailsTable.AddColumn("Name");
        detailsTable.AddColumn("Value");
        detailsTable.AddRow("Open", "");
        detailsTable.AddRow("High", "");
        detailsTable.AddRow("Low", "");
        detailsTable.AddRow("Close", "");

        detailsTable.AddEmptyRow();

        detailsTable.AddRow("base balance", "");
        detailsTable.AddRow("quote balance", "");


        var layout = new Layout("Root")
            .SplitColumns(
                new Layout("Left"),
                new Layout("Right")
            );


        string colour;
        BacktestCandle candle;

        await AnsiConsole.Live(layout)
            .StartAsync(async ctx =>
            {
                builder = builder
                    .OnTick(state =>
                    {
                        candle = state.GetCurrentCandle();
                        backtest.OnTick(state);
                        colour = candle.Close > candle.Open ? "[green]" : "[red]";
                        detailsTable.UpdateCell(0, 1, $"{colour}$ {candle.Open}[/]");
                        detailsTable.UpdateCell(1, 1, $"{colour}$ {candle.High}[/]");
                        detailsTable.UpdateCell(2, 1, $"{colour}$ {candle.Low}[/]");
                        detailsTable.UpdateCell(3, 1, $"{colour}$ {candle.Close}[/]");
                    })
                    .PostTick(e =>
                    {
                        if (e.CurrentCandleIndex == lastCandleIndex)
                        {
                            backtest.OnFinish(e);
                        }

                        detailsTable.UpdateCell(5, 1, $"{e.BaseBalance}");
                        detailsTable.UpdateCell(6, 1, $"{e.QuoteBalance}");

                        if (logTable.Rows.Count > 25)
                        {
                            logTable.RemoveRow(0);
                        }

                        layout["Left"]
                            .Update(logTable);
                        layout["Right"]
                            .Update(detailsTable);

                        ctx.Refresh();
                    })
                    .OnLogEntry((entry, _) =>
                    {
                        if (entry.Level > LogLevel.Information)
                        {
                            logTable.AddRow($"[red]{entry.Level}[/]", $"[red]{entry.Message}[/]");
                        }
                        else
                        {
                            logTable.AddRow(entry.Level.ToString(), entry.Message);
                        }
                    });
                backtestResult = await builder.RunAsync();
            });
        
        return backtestResult;
    }
}