using System.IO;

namespace Trl.TermDataRepresentation.Parser
{
    public interface ITrlParseResult
    {
        /// <summary>
        /// Used to deserialize a term to human readable form.
        /// </summary>
        void WriteToStream(StreamWriter outputStream);
    }
}
