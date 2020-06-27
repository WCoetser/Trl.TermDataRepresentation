using System.Collections.Generic;
using Trl.PegParser.Grammer.Semantics;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class GenericResult
        : GenericPassthroughResult<ITrlParseResult, TokenNames>, ITrlParseResult
    {
        internal IReadOnlyList<ITrlParseResult> GetSubResults() => SubResults;
    }
}
