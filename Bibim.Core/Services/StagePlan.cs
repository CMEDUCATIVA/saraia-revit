// Copyright (c) 2026 SquareZero Inc. - Licensed under Apache 2.0. See LICENSE in the repo root.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bibim.Core
{
    /// <summary>
    /// Parses the staging directive out of generated C#.
    ///
    /// Generated code declares its stages on a single comment line and then
    /// branches on <c>ctx.Stage</c>:
    ///
    /// <code>
    /// // ETAPAS: Explanada | Patas | Arcos | Plataformas
    /// switch (ctx.Stage) { case 0: ... break; case 1: ... break; }
    /// </code>
    ///
    /// The code is compiled ONCE and invoked once per stage. That matters for two
    /// reasons: splitting the source into separate compilations would break every
    /// variable shared across stages, and a single execution that looped over the
    /// stages internally would hold Revit's main thread for the whole build -
    /// which is exactly what makes Windows report the window as "Not Responding"
    /// and shows the user nothing at all.
    ///
    /// Unstaged code stays valid: no directive means a single stage, and the old
    /// one-shot behaviour is preserved.
    /// </summary>
    public static class StagePlan
    {
        /// <summary>Accepts ETAPAS / ETAPA / STAGES / STAGE, in any case.</summary>
        private static readonly Regex Directive = new Regex(
            @"^[ \t]*//[ \t]*(?:ETAPAS?|STAGES?)[ \t]*:[ \t]*(?<lista>.+)$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Hard ceiling so a malformed directive cannot spawn hundreds of executions.</summary>
        public const int MaxStages = 40;

        /// <summary>
        /// Returns the declared stage names, or a single default stage when the
        /// code carries no directive. Never returns null or an empty list.
        /// </summary>
        public static List<string> Parse(string code)
        {
            var names = new List<string>();
            if (string.IsNullOrWhiteSpace(code))
                return Single();

            var match = Directive.Match(code);
            if (!match.Success)
                return Single();

            foreach (var raw in match.Groups["lista"].Value.Split('|'))
            {
                string nombre = raw.Trim().Trim('"');
                if (nombre.Length > 0) names.Add(nombre);
            }

            if (names.Count == 0) return Single();
            if (names.Count > MaxStages) names = names.Take(MaxStages).ToList();
            return names;
        }

        /// <summary>True when the code declares more than one stage.</summary>
        public static bool IsStaged(string code) => Parse(code).Count > 1;

        /// <summary>Stage name for an index, with a safe fallback.</summary>
        public static string NameAt(IList<string> names, int index)
        {
            if (names != null && index >= 0 && index < names.Count)
                return names[index];
            return $"Etapa {index + 1}";
        }

        private static List<string> Single() => new List<string> { "Modelado" };
    }
}
