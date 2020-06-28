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

        public static string ToSourceCode(this ITrlParseResult @this)
        {
            using var memOut = new MemoryStream();
            using var streamWriter = new StreamWriter(memOut, Encoding.UTF8);

            @this.WriteToStream(streamWriter);
            streamWriter.Flush();
            memOut.Flush();
            
            return Encoding.UTF8.GetString(memOut.ToArray());
        }
    }
}
