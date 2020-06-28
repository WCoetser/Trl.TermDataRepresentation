using System;
using System.Collections.Generic;
using System.IO;
using Trl.PegParser.Grammer.Semantics;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class GenericResult
        : GenericPassthroughResult<ITrlParseResult, TokenNames>, ITrlParseResult
    {
        public void WriteToStream(StreamWriter outputStream)
        {
            // Not applicable
            throw new Exception();
        }

        internal IReadOnlyList<ITrlParseResult> GetSubResults() => SubResults;
    }
}
