﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using CorpusSearch.Model.Dictionary;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CorpusSearch.Service.Dictionaries;

public class CregeenDictionaryService(ISet<string> allWords, IList<CregeenEntry> entries) : ISearchDictionary
{
    public static readonly Dictionary<char, string> LetterLookup = new(new CaseInsensitiveCharComparer())
    {
        [' '] = "  ",
        ['A'] = "aa-",
        ['B'] = "baa",
        ['C'] = "caa",
        ['D'] = "da",
        ['E'] = "e",
        ['F'] = "fa.",
        ['G'] = "ga",
        ['H'] = "ha",
        ['I'] = "ick",
        ['J'] = "jaagh",
        ['K'] = "kaart",
        ['L'] = "laa",
        ['M'] = "maaig",
        ['N'] = "na",
        ['O'] = "O",
        ['P'] = "paa",
        ['Q'] = "quaagh",
        ['R'] = "raa",
        ['S'] = "saagh",
        ['T'] = "taagh",
        ['U'] = "udlan",
        ['V'] = "vaidjin",
        ['W'] = "wagaan",
        ['Y'] = "y",
    };

    public string Identifier => "Cregeen";
    public bool LinkToDictionary => true;
    public List<string> QueryLanguages => ["gv"];

    public static CregeenDictionaryService Init(ILogger<CregeenDictionaryService> log)
    {
        HashSet<string> allWords;

        IList<CregeenEntry> entries = new List<CregeenEntry>();
        try
        {
            entries = GetEntries();
            var allEntries = entries.SelectMany(x => x.ChildrenRecursive).SelectMany(x => x.Words);
            allWords = new HashSet<string>(allEntries, StringComparer.InvariantCultureIgnoreCase);
        } 
        catch (Exception)
        {
            // TODO: Add to health check
            log.LogError("Failed to load Cregeen");
            allWords = [];
        }
            
        return new CregeenDictionaryService(allWords, entries);
    }

    /// <summary>Loads the tree of entries from Cregeen</summary>
    public static IList<CregeenEntry> GetEntries()
    {
        // TODO: This shouldn't be static

        var path = Startup.GetLocalFile("Resources", "cregeen.json");
            
        var text = File.ReadAllText(path);

        var entries = JsonConvert.DeserializeObject<List<CregeenEntry>>(text) ?? [];

        entries.ForEach(EnrichCedillaEntries);
        return entries;
    }

    /// <summary>If an entry has 'ç', allow 'c'</summary>
    /// <remarks>Allows us to search for ymmyrçhagh. Shouldn't be done in the dictionary JSON. Best done here</remarks>
    private static void EnrichCedillaEntries(CregeenEntry entry)
    {
        // TODO: needs test
        entry.Words = entry.Words.Concat(entry.Words.Select(x => x.Replace('ç', 'c'))).ToHashSet().ToList();
        entry.SafeChildren.ForEach(EnrichCedillaEntries);
    }

    public static bool IsValidSearch(string query)
    {
        return !string.IsNullOrWhiteSpace(query) // invalid if whitespace
               && (query.Length > 1 // valid if longer than 1 char
                   || LetterLookup.ContainsKey(query[0]) // valid if 1 char and in the lookup
               );
    }

    public static IList<CregeenEntry> FuzzySearch(string query, IEnumerable<CregeenEntry> entries)
    {
        return FuzzySearchInternal(query, entries).Distinct().ToList();
    }

    private static IEnumerable<CregeenEntry> FuzzySearchInternal(string query, IEnumerable<CregeenEntry> entryData)
    {
        var flatEntries = entryData.SelectMany(x => x.ChildrenRecursive).ToList();

        // exact match
        foreach (var e in flatEntries.Where(x => x.ContainsWordExact(query)))
        {
            yield return e;
        }

        // Prefix
        foreach (var e in flatEntries.Where(entry => entry.Words.Any(word => word.StartsWith(query))))
        {
            yield return e;
        }

        // Contains
        foreach (var e in flatEntries.Where(entry => entry.Words.Any(word => word.Contains(query))))
        {
            yield return e;
        }
    }

    /// <summary>Whether the dictionary contains the provided word (no fuzziness)</summary>
    public bool ContainsWordExact(string s)
    {
        return allWords.Contains(s);
    }

    public IEnumerable<DictionarySummary> GetSummaries(string query, bool basic)
    {
        if (!ContainsWordExact(query)) { yield break; }

        // PERF: extract to member
        var entries1 = entries.SelectMany(x => x.ChildrenRecursive).ToList();

        foreach (var validEntry in entries1.Where(e => e.Words.Contains(query, StringComparer.InvariantCultureIgnoreCase)))
        {
            yield return GetDictionarySummary(validEntry);
        }

        yield break;

        string GetSummary(CregeenEntry entry)
        {
            if (basic && !string.IsNullOrWhiteSpace(entry.Definition))
            {
                return entry.Definition;
            }
                
            // decode the HTML
            HtmlDocument doc = new();
            doc.LoadHtml(entry.EntryHtml);

            return HttpUtility.HtmlDecode(doc.DocumentNode.InnerText);
        }

        DictionarySummary GetDictionarySummary(CregeenEntry entry)
        {
            return new DictionarySummary
            {
                PrimaryWord = entry.Words.First(),
                Summary = GetSummary(entry),
            };
        }
    }

    private class CaseInsensitiveCharComparer : IEqualityComparer<char>
    {
        public bool Equals(char x, char y) => char.ToUpperInvariant(x) == char.ToUpperInvariant(y);
        public int GetHashCode(char c) => char.ToUpperInvariant(c).GetHashCode();
    }
}