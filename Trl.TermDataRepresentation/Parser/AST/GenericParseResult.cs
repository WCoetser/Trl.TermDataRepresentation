using Trl.PegParser.Grammer.Semantics;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class GenericResult
        : GenericPassthroughResult<ITrlParseResult, TokenNames>, ITrlParseResult
    {
    }
}
