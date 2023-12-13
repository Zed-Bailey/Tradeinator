using System.Diagnostics;
using System.Reflection;
using CSScriptLib;
using Serilog;
using Tradeinator.Shared;

// initialise serilog logger, writing to console and file
await using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("strategies.log")
    .CreateLogger();



// loaded strategies
var loadedStrategies = new Dictionary<string, IStrategy>();


var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Strategies");

using var fileWatcher = new FileSystemWatcher(folderPath);
fileWatcher.EnableRaisingEvents = true;
fileWatcher.Filter = "*.cs";
fileWatcher.IncludeSubdirectories = true;
fileWatcher.NotifyFilter = NotifyFilters.Attributes
                       | NotifyFilters.CreationTime
                       | NotifyFilters.DirectoryName
                       | NotifyFilters.FileName
                       | NotifyFilters.LastAccess
                       | NotifyFilters.LastWrite
                       | NotifyFilters.Security
                       | NotifyFilters.Size;



fileWatcher.Changed += OnScriptChanged;
fileWatcher.Deleted += OnScriptDeleted;
fileWatcher.Created += OnScriptCreated;
fileWatcher.Renamed += OnScriptRenamed;
fileWatcher.Error += OnScriptError;



// load all the files
var existingStrategies = Directory.GetFiles(folderPath, "*.cs");
foreach (var file in existingStrategies)
{
    
    var name = Path.GetFileName(file);
    if (!loadedStrategies.ContainsKey(name))
    {
        
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        
        try
        {
            TryLoad(file);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception :(");
            Console.WriteLine(e);
        }
        
        stopWatch.Stop();
        
        logger.Information("Loaded strategy: {FileName} in {Time}ms. Strategy loaded from path: {Path}", name,stopWatch.ElapsedMilliseconds, file);
    }
    else
    {
        logger.Error("Loaded strategies already contains strategy {FileName}, strategy located at {Path} will not be loaded", name, file);
    }
}



Console.WriteLine(">> Press any key to exit");
Console.ReadLine();



void TryLoad(string file )
{
    // var scriptLines = File.ReadAllLines(file);
    // var scriptContent = string.Join('\n', scriptLines);
    var script = @"using System;
                public class Script
                {
                    public int Sum(int a, int b)
                    {
                        return a+b;
                    }
                }";
    // var evaluator = CSScript.Evaluator
    //     .ReferenceAssembliesFromCode(script)
    //     .ReferenceAssemblyByNamespace("Tradeinator.Shared")
    //     // .ReferenceAssembly(Assembly.GetExecutingAssembly())
    //     // .ReferenceAssembly(Assembly.GetExecutingAssembly().Location)
    //     .ReferenceDomainAssemblies();

        
        
    var s = CSScript.RoslynEvaluator.LoadCode<ICalc>(script);
    Console.WriteLine(s.Sum(1,2));
}


void OnScriptError(object sender, ErrorEventArgs e)
{
    logger.Error(e.GetException(), "An exception occured");
    logger.Error(e.GetException().InnerException, "Stack Trace: {StackTrace} Inner Exception: {InnerException}", e.GetException().StackTrace, e.GetException().InnerException);
}

void OnScriptRenamed(object sender, RenamedEventArgs e)
{
    if (loadedStrategies.ContainsKey(e.OldName))
    {
        var s = loadedStrategies[e.OldName];
        loadedStrategies[e.Name] = s;
        loadedStrategies.Remove(e.OldName);
        logger.Information("Strategy with old key: '{OldKey}' was already loaded, updated strategy to new key: '{NewKey}'", e.OldName, e.Name);
    }
}

void OnScriptCreated(object sender, FileSystemEventArgs e)
{
    throw new NotImplementedException();
}

void OnScriptDeleted(object sender, FileSystemEventArgs e)
{
    throw new NotImplementedException();
}

void OnScriptChanged(object sender, FileSystemEventArgs e)
{
    throw new NotImplementedException();
}

interface ICalc
{
    int Sum(int a, int b);
}