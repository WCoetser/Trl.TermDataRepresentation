using System.IO;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public class Variable : ITrlParseResult, ITrlTerm
    {
        /// <summary>
        /// Name of variable, can be . delimited.
        /// </summary>
        public string Name { get; set; }

        public void WriteToStream(StreamWriter outputStream)
        {
            outputStream.Write(Name);
        }
    }
}
