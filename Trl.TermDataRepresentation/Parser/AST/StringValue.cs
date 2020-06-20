using System.IO;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class StringValue : ITrlParseResult, ITrlTerm
    {
        public string Value { get; set; }

        public void WriteToStream(StreamWriter outputStream)
        {
            outputStream.Write($"\"{Value}\"");
        }
    }
}
