using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class TermList : ITrlParseResult, ITrlTerm
    {
        public IList<ITrlTerm> Terms;

        public void WriteToStream(StreamWriter outputStream)
        {
            outputStream.Write("(");
            if (Terms != null && Terms.Any())
            {
                var head = Terms.First();
                head.WriteToStream(outputStream);
                foreach (var tailItem in Terms.Skip(1))
                {
                    outputStream.Write(",");
                    tailItem.WriteToStream(outputStream);
                }
            }
            outputStream.Write(")");
        }
    }
}
