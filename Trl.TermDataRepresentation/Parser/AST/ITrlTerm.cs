using System.IO;

namespace Trl.TermDataRepresentation.Parser.AST
{
    public interface ITrlTerm
    {
        void WriteToStream(StreamWriter outputStream);
    }
}
