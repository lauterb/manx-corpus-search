﻿﻿using Codex_API.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codex_API.Model
{
    internal class CorpusSearchQuery
    {
        public string Query { get; }

        public CorpusSearchQuery(string query)
        {
            Query = query;
        }

        public bool Manx { get; internal set; }
        public bool English { get; internal set; }
        public bool FullText { get; internal set; }
        public DateTime MinDate { get; internal set; }
        public DateTime MaxDate { get; internal set; }
        public bool CaseSensitive { get; internal set; }

        internal bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Query) || Query.Length > 30)
            {
                return false;
            }
            if (!Manx && !English)
            {
                return false;
            }
            return true;
        }
    }
}