﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    internal class BuildLogSearchFilter : IEntryFilter
    {
        private readonly IEnumerable<IVsSearchToken> _searchTokens;
        private readonly IReadOnlyList<ITableColumnDefinition> _visibleColumns;

        public BuildLogSearchFilter(IVsSearchQuery searchQuery, IWpfTableControl control)
        {
            _searchTokens = SearchUtilities.ExtractSearchTokens(searchQuery) ?? new IVsSearchToken[] { };

            // TODO we need a better mechanism here to discover which columns are visible in the control.
            // Updating when entries change is reasonable but simply having something like VisibleColumns (with a VisibleColumnsChanged event)
            // on the table control would be better.
            //
            // As it stands, we will force any the packages that define a package to load if its columns are visible (but not currently active).
            var newVisibleColumns = control.ColumnStates
                .Where(c => c.IsVisible || (c as ColumnState2)?.GroupingPriority > 0)
                .Select(c => control.ColumnDefinitionManager.GetColumnDefinition(c.Name))
                .Where(definition => definition != null).ToList();

            _visibleColumns = newVisibleColumns;
        }

        public bool Match(ITableEntryHandle entry)
        {
            // An entry is considered matching a search query if all tokens in the search query are matching at least one of entry's columns.
            // Reserve one more column for details content
            var cachedColumnValues = new string[_visibleColumns.Count + 1];

            return _searchTokens.Where(searchToken => !(searchToken is IVsSearchFilterToken))
                .All(searchToken => AtLeastOneColumnOrDetailsContentMatches(entry, searchToken,
                    cachedColumnValues));
        }

        private bool AtLeastOneColumnOrDetailsContentMatches(ITableEntryHandle entry, IVsSearchToken searchToken, string[] cachedColumnValues)
        {
            // Check details content for any matches
            if (cachedColumnValues[0] == null)
            {
                cachedColumnValues[0] = GetDetailsContentAsString(entry);
            }

            var detailsContent = cachedColumnValues[0];
            if (detailsContent != null && Match(detailsContent, searchToken))
            {
                // Found match in details content
                return true;
            }

            // Check each column for any matches
            for (var i = 0; i < _visibleColumns.Count; i++)
            {
                if (cachedColumnValues[i + 1] == null)
                {
                    cachedColumnValues[i + 1] = GetColumnValueAsString(entry, _visibleColumns[i]);
                }

                var columnValue = cachedColumnValues[i + 1];
                System.Diagnostics.Debug.Assert(columnValue != null);

                if (columnValue != null && Match(columnValue, searchToken))
                {
                    // Found match in this column
                    return true;
                }
            }

            // No match found in this entry
            return false;
        }

        private static string GetColumnValueAsString(ITableEntryHandle entry, ITableColumnDefinition column) =>
            entry.TryCreateStringContent(column, truncatedText: false, singleColumnView: false, content: out string columnValue) && (columnValue != null)
                ? columnValue : string.Empty;

        private static string GetDetailsContentAsString(ITableEntryHandle entry)
        {
            string detailsString = null;

            if (entry.CanShowDetails && entry is IWpfTableEntry wpfEntry)
            {
                wpfEntry.TryCreateDetailsStringContent(out detailsString);
            }

            return detailsString ?? string.Empty;
        }

        private static bool Match(string columnValue, IVsSearchToken searchToken) =>
            columnValue != null && columnValue.IndexOf(searchToken.ParsedTokenText,
                StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
