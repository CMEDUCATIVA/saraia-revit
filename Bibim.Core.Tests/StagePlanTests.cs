using System.Linq;
using Bibim.Core;
using Xunit;

namespace Bibim.Core.Tests
{
    public class StagePlanTests
    {
        [Fact]
        public void Parse_ReadsDeclaredStagesInOrder()
        {
            string code = @"// ETAPAS: Cimentacion | Muros | Cubierta
switch (ctx.Stage) { case 0: break; }";

            var stages = StagePlan.Parse(code);

            Assert.Equal(new[] { "Cimentacion", "Muros", "Cubierta" }, stages);
            Assert.True(StagePlan.IsStaged(code));
        }

        [Fact]
        public void Parse_UnstagedCode_YieldsSingleStage()
        {
            string code = "var muros = new FilteredElementCollector(doc); return muros.GetElementCount();";

            var stages = StagePlan.Parse(code);

            Assert.Single(stages);
            Assert.False(StagePlan.IsStaged(code));
        }

        [Theory]
        [InlineData("// ETAPAS: A | B")]
        [InlineData("// ETAPA: A | B")]
        [InlineData("// STAGES: A | B")]
        [InlineData("//etapas:A|B")]
        [InlineData("    // Etapas :  A | B  ")]
        public void Parse_AcceptsDirectiveVariants(string directive)
        {
            var stages = StagePlan.Parse(directive + "\nswitch (ctx.Stage) { }");

            Assert.Equal(2, stages.Count);
            Assert.Equal("A", stages[0]);
            Assert.Equal("B", stages[1]);
        }

        [Fact]
        public void Parse_FindsDirectiveAfterLeadingComments()
        {
            string code = @"// Genera un edificio de dos plantas
// ETAPAS: Solera | Muros
switch (ctx.Stage) { }";

            Assert.Equal(new[] { "Solera", "Muros" }, StagePlan.Parse(code));
        }

        [Fact]
        public void Parse_IgnoresEmptyEntriesAndTrimsQuotes()
        {
            var stages = StagePlan.Parse("// ETAPAS: \"Solera\" |  | Muros |");

            Assert.Equal(new[] { "Solera", "Muros" }, stages);
        }

        [Fact]
        public void Parse_CapsRunawayStageCount()
        {
            string lista = string.Join(" | ", Enumerable.Range(0, 200).Select(i => "E" + i));

            var stages = StagePlan.Parse("// ETAPAS: " + lista);

            Assert.Equal(StagePlan.MaxStages, stages.Count);
        }

        [Fact]
        public void Parse_NullOrEmpty_IsSafe()
        {
            Assert.Single(StagePlan.Parse(null));
            Assert.Single(StagePlan.Parse(""));
            Assert.Single(StagePlan.Parse("   "));
        }

        [Fact]
        public void Parse_DirectiveWithoutNames_FallsBackToSingleStage()
        {
            Assert.Single(StagePlan.Parse("// ETAPAS:    |   |  "));
        }

        [Fact]
        public void NameAt_UsesDeclaredNameOrFallback()
        {
            var stages = StagePlan.Parse("// ETAPAS: Solera | Muros");

            Assert.Equal("Solera", StagePlan.NameAt(stages, 0));
            Assert.Equal("Muros", StagePlan.NameAt(stages, 1));
            Assert.Equal("Etapa 3", StagePlan.NameAt(stages, 2));   // fuera de rango
            Assert.Equal("Etapa 1", StagePlan.NameAt(null, 0));
        }
    }
}
