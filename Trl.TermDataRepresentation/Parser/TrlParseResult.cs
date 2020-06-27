using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.TermDataRepresentation.Parser
{
    public class TrlParseResult
    {
        public bool Succeed { get; set; }
        public StatementList Statements { get; set; }
    }
}
