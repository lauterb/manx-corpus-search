using CorpusSearch.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CorpusSearch.Controllers;
using CorpusSearch.Model;
using CorpusSearch.Dependencies.csly;
using CorpusSearch.Dependencies;
using CorpusSearch.Dependencies.Lucene;
using CorpusSearch.Service;
using CorpusSearch.Service.Dictionaries;
using CorpusSearch.Utils;
using Microsoft.Extensions.Logging;
using Serilog;
using static System.Text.Json.JsonSerializer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CorpusSearch;

public class LoadConfig
{
    //add below to appsettings.Development.json for fast load
    //},
    //"Loading": {
    //    "OpenDataOnly":  true,
    //    "VideoOnly": true,
    //    "MaxOpenData": 1
    //}
    public bool VideoOnly { get; set;}
    public bool OpenDataOnly { get; set;}
    public int MaxOpenData { get; set;}
    //public LoadConfig(bool videoOnlyConfig) => videoOnlyConfig = videoOnly;
}

public class Startup(IConfiguration configuration)
{
    public static Dictionary<string, IList<string>> EnglishToManxDictionary { get; set; }
    public static Dictionary<string, IList<string>> ManxToEnglishDictionary { get; set; }


    public IConfiguration Configuration { get; } = configuration;

    private ILogger<Startup> log;

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        services.AddSingleton(provider => LuceneIndex.GetInstance());
        services.AddSingleton(provider => SearchParser.GetParser());
        services.AddSingleton<Searcher>();
        services.AddSingleton(provider => CregeenDictionaryService.Init(provider.GetService<ILogger<CregeenDictionaryService>>()));
        services.AddSingleton<ISearchDictionary>(provider => provider.GetService<CregeenDictionaryService>());
        services.AddSingleton(provider => KellyManxToEnglishDictionaryService.Init(provider.GetService<ILogger<KellyManxToEnglishDictionaryService>>()));
        services.AddSingleton<ISearchDictionary>(provider => provider.GetService<KellyManxToEnglishDictionaryService>());
        services.AddSingleton<WorkService>();
        services.AddSingleton<DocumentSearchService>();
        services.AddSingleton<NewspaperSourceEnricher>();
        services.AddSingleton<OverviewSearchService2>();
        // TODO: Move config here
        services.AddSingleton<RecentDocumentsService>();

        // In production, the React files will be served from this directory
        services.AddSpaStaticFiles(configuration =>
        {
            configuration.RootPath = "ClientApp/build";
        });
    }

    public void Configure(IApplicationBuilder app,
        IWebHostEnvironment env,
        WorkService workService,
        ILogger<Startup> logger,
        Searcher searcher,
        RecentDocumentsService recentDocumentsService)
    {
        log = logger;
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        if (!AnonymousAnalytics.Init())
        {
            log.LogWarning("Failed to init anonymous analytics. Was CORPUS_SEARCH_SEGMENT_KEY set?");
        }
        var lConfig = Configuration.GetSection("Loading").Get<LoadConfig>();

        var databaseCount = SetupDatabase(workService, searcher, lConfig);
        var termFrequency = searcher.QueryTermFrequency();
        StatisticsController.Init(databaseCount, termFrequency, log);
        SetupDictionaries();

        try
        {
            var latestDocuments = OpenDataLoader.LoadRecentDocuments(workService).Result;
            recentDocumentsService.Init(latestDocuments, log);
        }
        catch (Exception e)
        {
            log.LogError(e , "failed to read latest documents");
        }
            
        // app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseSpaStaticFiles();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller}/{action=Index}/{id?}");
        });

        app.UseSpa(spa =>
        {
            spa.Options.SourcePath = "ClientApp";

            if (env.IsDevelopment())
            {
                spa.UseReactDevelopmentServer(npmScript: "start");
            }
        });

        GC.Collect();
    }

    internal static void SetupDictionaries()
    {
        // This saves ~700MB RAM compared to using F# for XML reading... sorry
        // files sourced from Phil Kelly https://www.learnmanx.com/page_342285.html
        using (FileStream manx = File.OpenRead(GetLocalFile("Resources", "manx.json")))
        {
            ManxToEnglishDictionary = ToCaseInsensitiveDict(manx);
        }
        using (FileStream english = File.OpenRead(GetLocalFile("Resources", "english.json")))
        {
            EnglishToManxDictionary = ToCaseInsensitiveDict(english);
        }

        return;

        Dictionary<string, IList<string>> ToCaseInsensitiveDict(FileStream fileStream) 
        {
            var dict = DeserializeAsync<Dictionary<string, IList<string>>>(fileStream).Result;
            return new Dictionary<string, IList<string>>(dict, StringComparer.OrdinalIgnoreCase);
        }
    }

    internal (long totalDocuments, long totalManxTerms) SetupDatabase(WorkService workService, Searcher searcher, LoadConfig lConfig)
    {
        var totalDocuments = 0L;

        bool ignoreClosedData = lConfig?.OpenDataOnly ?? false;
        if (!ignoreClosedData) try
            {
                List<Document> closedSourceDocument = ClosedDataLoader.LoadDocumentsFromFile().Cast<Document>().ToList();
                log.LogInformation("Loaded {Count} documents", closedSourceDocument.Count);
                AddDocuments(closedSourceDocument, workService, searcher);
                totalDocuments += closedSourceDocument.Count;
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed loading documents");
            }
        // Try adding open source documents
        try
        {
            List<Document> ossDocuments = OpenDataLoader.LoadDocumentsFromFile(lConfig).Cast<Document>().ToList();
            log.LogInformation("Loaded {OssDocumentsCount} documents", ossDocuments.Count);
            AddDocuments(ossDocuments, workService, searcher);
            totalDocuments += ossDocuments.Count;
        }
        catch (Exception e)
        {
            log.LogError(e, "Failed loading documents");
        }

        var stopWatch = System.Diagnostics.Stopwatch.StartNew();
        searcher.OnAllDocumentsAdded();
        log.LogDebug("compacted in {CompactedInMilliseconds}", stopWatch.ElapsedMilliseconds);

        var totalTerms = searcher.CountManxTerms();
        return (totalDocuments, totalTerms);
    }

    private static void AddDocuments(List<Document> documents, WorkService workService, Searcher searcher)
    {
        foreach (var document in documents)
        {
            AddDocument(document, workService, searcher);
        }
    }

    public static string GetLocalFile(params string[] inputPath)
    {
        String[] path = new string[inputPath.Length + 1];
        path[0] = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < inputPath.Length; i++)
        {
            path[i + 1] = inputPath[i];
        }

        return Path.Combine(path);
    }

    private static void AddDocument(Document document, WorkService workService, Searcher searcher)
    {
        List<DocumentLine> data = document.LoadLocalFile();

        searcher.AddDocument(document, data);

        workService.AddWork(document);
    }
}