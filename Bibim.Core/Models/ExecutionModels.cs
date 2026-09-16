// Copyright (c) 2026 SquareZero Inc. â€” Licensed under Apache 2.0. See LICENSE in the repo root.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Bibim.Core
{
    public enum ExecutionRequestKind
    {
        ExecuteCode,
        UndoLastApply
    }

    /// <summary>
    /// Execution request queued from background thread for main-thread execution.
    /// See design doc §2.3 — ExternalEvent main thread synchronization.
    /// </summary>
    public class ExecutionRequest
    {
        public ExecutionRequestKind Kind { get; set; } = ExecutionRequestKind.ExecuteCode;

        /// <summary>Roslyn-compiled assembly containing the generated code.</summary>
        public Assembly CompiledAssembly { get; set; }

        /// <summary>Entry point type name within the compiled assembly.</summary>
        public string EntryTypeName { get; set; } = "BibimGenerated.Program";

        /// <summary>Entry point method name.</summary>
        public string EntryMethodName { get; set; } = "Execute";

        /// <summary>If true, execute in a Transaction that is always rolled back.</summary>
        public bool IsDryRun { get; set; }

        /// <summary>Expected target document title captured during preview.</summary>
        public string ExpectedDocumentTitle { get; set; }

        /// <summary>Expected target document path captured during preview.</summary>
        public string ExpectedDocumentPath { get; set; }

        /// <summary>
        /// Which stage of a staged plan to run. -1 (default) runs every stage
        /// sequentially inside this single execution - used by dry runs, where
        /// the stages must accumulate before the group rolls back. A value >= 0
        /// runs just that stage, which is how a visible staged apply works: one
        /// execution per stage, so Revit regains its message loop and repaints
        /// in between.
        /// </summary>
        public int StageIndex { get; set; } = -1;

        /// <summary>Total number of stages in the plan. 1 when the code is not staged.</summary>
        public int StageCount { get; set; } = 1;

        /// <summary>Display names of the stages, in order. May be null.</summary>
        public List<string> StageNames { get; set; }

        /// <summary>
        /// When true, export the active view to PNG while the execution's
        /// TransactionGroup is still open - i.e. BEFORE a dry-run rolls back.
        /// This is what lets a caller SEE a simulation without altering the model.
        /// </summary>
        public bool CaptureImage { get; set; }

        /// <summary>Pixel width for <see cref="CaptureImage"/>. 0 = exporter default.</summary>
        public int CaptureWidth { get; set; }

        /// <summary>Callback to return result to the awaiting background thread.</summary>
        public TaskCompletionSource<ExecutionResult> Callback { get; set; }
    }

    /// <summary>
    /// Result of code execution on the Revit main thread.
    /// </summary>
    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public int AffectedElementCount { get; set; }
        public long MemoryBefore { get; set; }
        public long MemoryAfter { get; set; }
        public string DocumentTitle { get; set; }
        public string DocumentPath { get; set; }

        /// <summary>
        /// Revit warnings collected via Application.FailuresProcessing during execution.
        /// Populated only when warnings fire during commit/dryrun.
        /// </summary>
        public List<string> RevitWarnings { get; set; }

        /// <summary>
        /// Intermediate log entries written by generated code via BibimExecutionContext.Log().
        /// </summary>
        public List<string> ExecutionLogs { get; set; }

        /// <summary>
        /// Base64 PNG of the active view captured during execution, when the
        /// request asked for it. On a dry run this shows the SIMULATED state -
        /// the geometry is rolled back immediately after the capture.
        /// </summary>
        public string ViewImageBase64 { get; set; }

        /// <summary>Why the capture failed, when one was requested but produced no image.</summary>
        public string CaptureError { get; set; }

        public bool HasViewImage => !string.IsNullOrEmpty(ViewImageBase64);

        /// <summary>Stages actually applied by this execution (1 when not staged).</summary>
        public int StagesApplied { get; set; } = 1;

        /// <summary>
        /// Milliseconds this execution held Revit's main thread. Anything above a
        /// couple of seconds makes Windows paint the window as "Not Responding",
        /// so callers get this back to keep their stages short.
        /// </summary>
        public long MainThreadMs { get; set; }

        public bool HasRevitWarnings => RevitWarnings != null && RevitWarnings.Count > 0;
        public bool HasExecutionLogs => ExecutionLogs != null && ExecutionLogs.Count > 0;
    }

    /// <summary>
    /// Context object injected into generated code as second parameter.
    /// Generated code calls ctx.Log() to emit intermediate progress messages
    /// that appear in the Bibim panel after execution.
    ///
    /// Signature: public static object Execute(UIApplication uiApp, Bibim.Core.BibimExecutionContext ctx)
    /// </summary>
    public class BibimExecutionContext
    {
        private readonly List<string> _logs = new List<string>();

        /// <summary>
        /// Index of the stage being executed, 0-based. Generated code branches on
        /// this (switch (ctx.Stage)) so one compiled assembly can build a model in
        /// visible steps. Always 0 when the code is not staged.
        /// </summary>
        public int Stage { get; internal set; }

        /// <summary>Total stages in the plan. 1 when the code is not staged.</summary>
        public int StageCount { get; internal set; } = 1;

        /// <summary>Display name of the current stage, when the plan declared one.</summary>
        public string StageName { get; internal set; }

        public void Log(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                _logs.Add(message);
        }

        public IReadOnlyList<string> GetLogs() => _logs.AsReadOnly();
    }

    internal static class ExecutionResultFormatter
    {
        public static string BuildDryRunOutput(object generatedOutput, int affectedElementCount)
        {
            string text = generatedOutput?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
                return text;

            return $"Dry run succeeded. {affectedElementCount} elements would be affected.";
        }
    }
}
