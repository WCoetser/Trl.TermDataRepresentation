using System;
using System.IO;
using System.Text;
using Trl.Serialization.Translator;
using Trl.TermDataRepresentation.Parser;

namespace Trl.Serialization
{
    public class TextStreamSerializer
    {
        private readonly Encoding _encoding;
        private readonly ObjectTranslator _translator;

        public TextStreamSerializer(Encoding encoding)
            => (_encoding, _translator) = (encoding, new ObjectTranslator());

        public TObject Deserialize<TObject>(StreamReader input, string rootLabel = "root")
        {
            var inputStr = input.ReadToEnd();
            return _translator.BuildObject<TObject>(inputStr, rootLabel);
        }

        public void Serialize<TObject>(TObject inputObject, StreamWriter outputStream, string rootLabel = "root")
        {
            ITrlParseResult output = _translator.BuildAst(inputObject, rootLabel);
            output.WriteToStream(outputStream);
        }
    }
}
