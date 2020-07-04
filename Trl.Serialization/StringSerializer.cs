using System.IO;
using System.Text;

namespace Trl.Serialization
{
    public class StringSerializer
    {
        private readonly TextStreamSerializer _stringStreamSerializer;
        private readonly Encoding _encoding;

        public StringSerializer(Encoding encoding = null)
        {
            _encoding = encoding ?? Encoding.Default;
            _stringStreamSerializer = new TextStreamSerializer(_encoding);
        }

        public TObject Deserialize<TObject>(string input, string rootLabel = "root")
        {
            using var memIn = new MemoryStream();
            using var streamWriter = new StreamWriter(memIn, _encoding);
            streamWriter.Write(input);
            streamWriter.Flush();
            memIn.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(memIn, _encoding);
            return _stringStreamSerializer.Deserialize<TObject>(streamReader, rootLabel);
        }

        public string Serialize<TObject>(TObject inputObject, string rootLabel = "root")
        {
            using var memOut = new MemoryStream();
            using var outputStream = new StreamWriter(memOut, _encoding);
            _stringStreamSerializer.Serialize(inputObject, outputStream, rootLabel);
            outputStream.Flush();
            return _encoding.GetString(memOut.ToArray());
        }
    }
}
