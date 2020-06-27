using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class Label : ITrlParseResult
    {
        public List<Identifier> Identifiers { get; set; }

        public IReadOnlyList<ITrlParseResult> SubResults { get => throw new System.Exception(); }
    }
}
