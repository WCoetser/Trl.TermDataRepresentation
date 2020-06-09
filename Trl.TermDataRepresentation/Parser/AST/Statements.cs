using System.Collections.Generic;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class Statements : ITrlParseResult
    {
        public List<ITrlParseResult> StatementList { get; set; }
    }
}
