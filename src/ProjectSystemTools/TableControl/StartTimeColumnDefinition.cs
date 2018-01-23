﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(TableColumnNames.StartTime)]
    internal sealed class StartTimeColumnDefinition : TableColumnDefinitionBase
    {
        public override string Name => TableColumnNames.StartTime;

        public override string DisplayName => TableControlResources.StartTimeHeaderLabel;

        public override StringComparer Comparer => StringComparer.Ordinal;

        public override double MinWidth => 100.0;

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string content)
        {
            if (entry.TryGetValue(TableKeyNames.StartTime, out var value) && value != null && value is DateTime startTime)
            {
                content = startTime.ToString("s");
                return true;
            }

            content = null;
            return false;
        }
    }
}
