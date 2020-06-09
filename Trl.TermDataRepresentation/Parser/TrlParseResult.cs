using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.TermDataRepresentation.Parser
{
    public class TrlParseResult
    {
        public bool Succeed { get; set; }
        public Statements Statements { get; set; }
    }
}
