using System.IO;
using Trl.PegParser.Grammer.Semantics;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class RewriteRule : GenericPassthroughResult<ITrlParseResult, TokenNames>, ITrlParseResult
    {
        public ITrlTerm MatchTerm { get; set; }

        public ITrlTerm SubstituteTerm { get; set; }

        public void WriteToStream(StreamWriter outputStream)
        {
            MatchTerm.WriteToStream(outputStream);
            outputStream.Write(" => ");
            SubstituteTerm.WriteToStream(outputStream);
            outputStream.Write(";");
        }
    }
}
