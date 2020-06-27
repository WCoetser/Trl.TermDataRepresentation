using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class StatementList : ITrlParseResult
    {
        public List<Statement> Statements { get; set; }
    }
}
