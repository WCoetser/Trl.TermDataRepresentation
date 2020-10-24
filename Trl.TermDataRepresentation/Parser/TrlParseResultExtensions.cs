using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.TermDataRepresentation.Parser
{
    public static class TrlParseResultExtensions
    {
        /// <summary>
        /// Utility method to reduce type casting.
        /// </summary>
        internal static IReadOnlyList<ITrlParseResult> GetSubResults(this ITrlParseResult @this)
            => ((GenericResult)@this).SubResults.Cast<ITrlParseResult>().ToList().AsReadOnly();
    }
}
