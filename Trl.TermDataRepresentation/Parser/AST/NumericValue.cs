using System.IO;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class NumericValue : ITrlParseResult, ITrlTerm
    {
        public string Value { get; set; }

        public void WriteToStream(StreamWriter outputStream)
        {
            outputStream.Write(Value);
        }
    }
}
