﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class EvaluatedPassViewModel : BaseViewModel
    {
        private readonly EvaluatedPass _evaluatedPass;
        private string _text;
        private List<object> _children;
        //private SelectedObjectWrapper _properties;

        public override string Text => _text ?? (_text = _evaluatedPass.Description);

        public override IEnumerable<object> Children => _children ?? (_children = GetChildren());

        //public override SelectedObjectWrapper Properties => _properties ?? (_properties =
        //    new SelectedObjectWrapper(
        //        _evaluatedProject.Name,
        //        "Evaluated Project",
        //        _evaluation?.Messages.Union(_evaluatedProject.Messages) ?? _evaluatedProject.Messages,
        //        new Dictionary<string, IDictionary<string, string>> {
        //            {"Build", new Dictionary<string, string>
        //                {
        //                    {"Started", _evaluatedProject.StartTime.ToString(CultureInfo.InvariantCulture)},
        //                    {"Finished", _evaluatedProject.EndTime.ToString(CultureInfo.InvariantCulture)}
        //                }
        //             }
        //        }));

        public EvaluatedPassViewModel(EvaluatedPass evaluatedPass)
        {
            _evaluatedPass = evaluatedPass;
        }

        private List<object> GetChildren() => ((IEnumerable<object>)_evaluatedPass.Locations.Select(location => new EvaluatedLocationViewModel(location))).ToList();
    }
}
