﻿using Codex_API.Service;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Codex_API.Dependencies.Lucene
{
    /// <summary>
    /// Extends WildcardQuery to handle a "+"
    /// </summary>
    public class ExtendedWildcardQuery: AutomatonQuery
    {
        public static ISet<char> RelevantChars = new HashSet<char>() { '+', '?', '*' };

        public ExtendedWildcardQuery(Term term) : base(GetTerm(term), ToAutomaton(term))
        {
        }

        private static Term GetTerm(Term term)
        {
            return new Term(term.Field, DiacriticService.Replace(term.Bytes.Utf8ToString()));
        }

        /// <summary>
        /// Returns the pattern term.
        /// </summary>
        public virtual Term Term => base.m_term;

        private static Automaton ToAutomaton(Term term)
        {
            IList<Automaton> automata = new List<Automaton>();

            string wildcardText = term.Text();

            for (int i = 0; i < wildcardText.Length; i++)
            {
                char c = wildcardText[i];

                // any string
                if (c ==  '*')
                {
                    automata.Add(BasicAutomata.MakeAnyString());
                    continue;
                }
                
                // single char
                if (c ==  '?')
                {
                    automata.Add(BasicAutomata.MakeAnyChar());
                    continue;
                }


                if (c == '+')
                {
                    automata.Add(BasicAutomata.MakeAnyChar());
                    automata.Add(BasicAutomata.MakeAnyString());
                    continue;
                }

                automata.Add(BasicAutomata.MakeChar(c));
            }

            return BasicOperations.Concatenate(automata);
        }

        public static bool AppliesTo(string value)
        {
            return value.Any(RelevantChars.Contains);
        }
    }
}