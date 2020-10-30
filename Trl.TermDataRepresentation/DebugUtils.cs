using System.IO;
using System.Text;
using Trl.TermDataRepresentation.Database;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.TermDataRepresentation
{
    public static class DebugUtils
    {
        public static string ToSourceCode(this Substitution substitution)
        {
            var reader = substitution.TermDatabase.Reader;
            var head = reader.ReadTerm(substitution.MatchTerm).ToSourceCode();
            var tail = reader.ReadTerm(substitution.SubstituteTerm).ToSourceCode();
            return $"{head} => {tail};";
        }

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
