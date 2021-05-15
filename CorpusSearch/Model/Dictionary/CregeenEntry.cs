﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpusSearch.Model.Dictionary
{
    /// <summary>
    /// JSON Model for the cregeen dictionary
    /// </summary>
    public class CregeenEntry
    {
        public List<string> Words { get; set; }
        public string EntryHtml { get; set; }
        public string HeadingHtml { get; set; }
        // nullable
        public List<CregeenEntry> Children { get; set; }

        public List<CregeenEntry> SafeChildren => Children ?? new List<CregeenEntry>();

        public List<CregeenEntry> ChildrenRecursive => new[] { this }.Concat(SafeChildren.SelectMany(x => x.ChildrenRecursive)).ToList();

        public IList<CregeenEntry> FilterTo(string search)
        {
            if (Words.Any(x => x == search))
            {
                return new[] { this };
            }

            var children = SafeChildren.SelectMany(x => x.FilterTo(search)).ToList();

            if (!children.Any())
            {
                return Array.Empty<CregeenEntry>();
            }

            return new[]
            {
                new CregeenEntry
                {
                    Words = this.Words,
                    EntryHtml = this.EntryHtml,
                    HeadingHtml = this.HeadingHtml,
                    Children = children,
                }
            };
        }
    }
}