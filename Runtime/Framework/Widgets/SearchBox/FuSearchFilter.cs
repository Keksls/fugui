using System;

namespace Fu.Framework
{
    /// <summary>
    /// Small reusable text filter for Fugui search-driven widgets.
    /// </summary>
    public class FuSearchFilter
    {
        #region State
        private const int SearchTermCacheCapacity = 256;
        private static readonly char[] _termSeparators = { ' ', '\t', '\r', '\n' };
        private static readonly FuBoundedCache<string, string[]> _searchTerms =
            new FuBoundedCache<string, string[]>(SearchTermCacheCapacity, StringComparer.Ordinal);
        public string Query { get; set; } = string.Empty;
        public bool CaseSensitive { get; set; } = false;
        public bool MatchAllTerms { get; set; } = true;

        public bool IsActive => !string.IsNullOrWhiteSpace(Query);
        #endregion

        #region Methods
        /// <summary>
        /// Reset the current filter query.
        /// </summary>
        public void Clear()
        {
            Query = string.Empty;
        }

        /// <summary>
        /// Check whether one text value passes this filter.
        /// </summary>
        /// <param name="text">Text value to test.</param>
        /// <returns>true if the value matches the current query.</returns>
        public bool Passes(string text)
        {
            return PassesSingle(Query, CaseSensitive, MatchAllTerms, text);
        }

        /// <summary>
        /// Check whether a row made of multiple text values passes this filter.
        /// </summary>
        /// <param name="values">Text values to test as one searchable row.</param>
        /// <returns>true if the values match the current query.</returns>
        public bool Passes(params string[] values)
        {
            return Passes(Query, CaseSensitive, MatchAllTerms, values);
        }

        /// <summary>
        /// Check whether one text value passes a case-insensitive all-terms query.
        /// </summary>
        /// <param name="query">Search query to evaluate.</param>
        /// <param name="text">Text value to test.</param>
        /// <returns>true if the value matches the query.</returns>
        public static bool Passes(string query, string text)
        {
            return PassesSingle(query, false, true, text);
        }

        /// <summary>
        /// Check whether a group of text values passes a query.
        /// </summary>
        /// <param name="query">Search query split on whitespace.</param>
        /// <param name="caseSensitive">Use ordinal case-sensitive matching when true.</param>
        /// <param name="matchAllTerms">Require every query term when true, or any term when false.</param>
        /// <param name="values">Text values to test as one searchable row.</param>
        /// <returns>true if the values match the query settings.</returns>
        public static bool Passes(string query, bool caseSensitive, bool matchAllTerms, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            string[] terms = GetSearchTerms(query);
            if (terms.Length == 0)
            {
                return true;
            }

            StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (matchAllTerms)
            {
                for (int termIndex = 0; termIndex < terms.Length; termIndex++)
                {
                    if (!ContainsAny(values, terms[termIndex], comparison))
                    {
                        return false;
                    }
                }
                return true;
            }

            for (int termIndex = 0; termIndex < terms.Length; termIndex++)
            {
                if (ContainsAny(values, terms[termIndex], comparison))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks one text value without creating a params array.
        /// </summary>
        /// <param name="query">Search query to evaluate.</param>
        /// <param name="caseSensitive">Use ordinal case-sensitive matching when true.</param>
        /// <param name="matchAllTerms">Require every term when true, or any term when false.</param>
        /// <param name="text">Single text value to inspect.</param>
        /// <returns>True when the text matches the query settings.</returns>
        private static bool PassesSingle(string query, bool caseSensitive, bool matchAllTerms, string text)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            string[] terms = GetSearchTerms(query);
            if (terms.Length == 0)
            {
                return true;
            }

            StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (matchAllTerms)
            {
                for (int termIndex = 0; termIndex < terms.Length; termIndex++)
                {
                    if (string.IsNullOrEmpty(text) || text.IndexOf(terms[termIndex], comparison) < 0)
                    {
                        return false;
                    }
                }

                return true;
            }

            for (int termIndex = 0; termIndex < terms.Length; termIndex++)
            {
                if (!string.IsNullOrEmpty(text) && text.IndexOf(terms[termIndex], comparison) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets normalized search terms without splitting a stable query again.
        /// </summary>
        /// <param name="query">Search query to parse.</param>
        /// <returns>Cached non-empty query terms.</returns>
        private static string[] GetSearchTerms(string query)
        {
            // Query parsing allocates only on a bounded cache miss.
            if (!_searchTerms.TryGetValue(query, out string[] terms))
            {
                terms = query.Split(_termSeparators, StringSplitOptions.RemoveEmptyEntries);
                _searchTerms.Set(query, terms);
            }

            return terms;
        }

        /// <summary>
        /// Clears parsed search terms owned by the current Fugui session.
        /// </summary>
        internal static void ResetSearchTermCache()
        {
            // Cached arrays retain query substrings and must follow the runtime lifetime.
            _searchTerms.Clear();
        }

        /// <summary>
        /// Test whether one normalized search term is present in any provided value.
        /// </summary>
        /// <param name="values">Values to search.</param>
        /// <param name="term">Single search term.</param>
        /// <param name="comparison">String comparison mode to use.</param>
        /// <returns>true if the term is found in at least one value.</returns>
        private static bool ContainsAny(string[] values, string term, StringComparison comparison)
        {
            if (values == null)
            {
                return false;
            }

            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                if (!string.IsNullOrEmpty(value) && value.IndexOf(term, comparison) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
