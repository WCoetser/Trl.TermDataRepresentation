using System.Collections.Generic;
using System.Linq;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.TermDataRepresentation.Parser
{
    internal static class TrlParseResultExtensions
    {
        /// <summary>
        /// Utility method to reduce type casting.
        /// </summary>
        internal static IReadOnlyList<ITrlParseResult> GetSubResults(this ITrlParseResult @this)
            => ((GenericResult)@this).SubResults.Cast<ITrlParseResult>().ToList().AsReadOnly();
    }
}
