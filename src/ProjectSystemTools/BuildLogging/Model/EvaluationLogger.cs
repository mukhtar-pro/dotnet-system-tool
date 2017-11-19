﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    internal sealed class EvaluationLogger : LoggerBase
    {
        private sealed class EventWrapper : IEventSource
        {
            public BinaryLogger BinaryLogger { get; }

            public event BuildMessageEventHandler MessageRaised { add { } remove { } }
            public event BuildErrorEventHandler ErrorRaised { add { } remove { } }
            public event BuildWarningEventHandler WarningRaised { add { } remove { } }
            public event BuildStartedEventHandler BuildStarted { add { } remove { } }
            public event BuildFinishedEventHandler BuildFinished { add { } remove { } }
            public event ProjectStartedEventHandler ProjectStarted { add { } remove { } }
            public event ProjectFinishedEventHandler ProjectFinished { add { } remove { } }
            public event TargetStartedEventHandler TargetStarted { add { } remove { } }
            public event TargetFinishedEventHandler TargetFinished { add { } remove { } }
            public event TaskStartedEventHandler TaskStarted { add { } remove { } }
            public event TaskFinishedEventHandler TaskFinished { add { } remove { } }
            public event CustomBuildEventHandler CustomEventRaised { add { } remove { } }
            public event BuildStatusEventHandler StatusEventRaised { add { } remove { } }
            public event AnyEventHandler AnyEventRaised;

            public EventWrapper(BinaryLogger binaryLogger)
            {
                BinaryLogger = binaryLogger;
                BinaryLogger.Initialize(this);
            }

            public void RaiseEvent(object sender, BuildEventArgs args) => AnyEventRaised?.Invoke(sender, args);
        }

        private readonly Dictionary<int, (EventWrapper, Build, string)> _evaluations = new Dictionary<int, (EventWrapper, Build, string)>();

        public EvaluationLogger(BuildTableDataSource dataSource) :
            base(dataSource)
        {
        }

        public override void Initialize(IEventSource eventSource)
        {
            eventSource.AnyEventRaised += AnyEvent;
        }

        private void AnyEvent(object sender, BuildEventArgs args)
        {
            if (args.BuildEventContext.EvaluationId == BuildEventContext.InvalidEvaluationId)
            {
                return;
            }

            switch (args)
            {
                case ProjectEvaluationStartedEventArgs evaluationStarted:
                    {
                        if (!DataSource.IsLogging || evaluationStarted.ProjectFile == "(null)")
                        {
                            return;
                        }

                        var logPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.binlog");
                        var binaryLogger = new BinaryLogger
                        {
                            Parameters = logPath,
                            Verbosity = LoggerVerbosity.Diagnostic,
                            CollectProjectImports = BinaryLogger.ProjectImportsCollectionMode.None
                        };
                        var wrapper = new EventWrapper(binaryLogger);
                        var build = new Build(evaluationStarted.ProjectFile, Array.Empty<string>(), Array.Empty<string>(), 
                            BuildType.Evaluation, args.Timestamp);
                        _evaluations[evaluationStarted.BuildEventContext.EvaluationId] = (wrapper, build, logPath);
                        wrapper.RaiseEvent(sender, args);
                        DataSource.AddEntry(build);
                    }
                    break;

                case ProjectEvaluationFinishedEventArgs evaluationFinished:
                    {
                        if (_evaluations.TryGetValue(args.BuildEventContext.EvaluationId, out var tuple))
                        {
                            var (wrapper, build, logPath) = tuple;
                            build.Finish(true, args.Timestamp, logPath);
                            wrapper.RaiseEvent(sender, args);
                            wrapper.BinaryLogger.Shutdown();
                            DataSource.NotifyChange();
                        }
                    }
                    break;

                default:
                    {
                        if (_evaluations.TryGetValue(args.BuildEventContext.EvaluationId, out var tuple))
                        {
                            var (wrapper, build, logPath) = tuple;
                            wrapper.RaiseEvent(sender, args);
                        }
                    }
                    break;
            }
        }
    }
}
