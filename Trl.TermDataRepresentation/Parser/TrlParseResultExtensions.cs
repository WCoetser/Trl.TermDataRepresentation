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

        public static string ToSourceCode(this ITrlParseResult @this, bool prettyPrint = false)
        {
            using var memOut = new MemoryStream();
            using var streamWriter = new StreamWriter(memOut, Encoding.UTF8);

            if (@this is StatementList)
            {
                ((StatementList)@this).WriteToStream(streamWriter, prettyPrint);
            }
            else
            {
                @this.WriteToStream(streamWriter);
            }

            streamWriter.Flush();
            memOut.Flush();
            memOut.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(memOut, Encoding.UTF8);
            return streamReader.ReadToEnd();
        }
    }
}
