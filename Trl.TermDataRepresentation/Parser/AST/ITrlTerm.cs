using System.IO;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public interface ITrlTerm
    {
        /// <summary>
        /// Used to deserialize a term to human readable form.
        /// </summary>
        void WriteToStream(StreamWriter outputStream);        
    }
}
