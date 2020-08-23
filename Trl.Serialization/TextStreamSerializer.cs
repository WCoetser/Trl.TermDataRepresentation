﻿using System;
using System.IO;
using System.Text;
using Trl.Serialization.Translator;
using Trl.TermDataRepresentation.Parser;

namespace Trl.Serialization
{
    public class TextStreamSerializer
    {
        private readonly Encoding _encoding;
        private readonly ObjectToAstTranslator _objectToAstTranslator;
        private readonly StringToObjectTranslator _stringToObjectTranslator;

        public TextStreamSerializer(Encoding encoding)
        {
            _encoding = encoding;
            _objectToAstTranslator = new ObjectToAstTranslator();
            _stringToObjectTranslator = new StringToObjectTranslator();
        }

        public TObject Deserialize<TObject>(StreamReader input, string rootLabel = "root", int maxRewriteIterations = 100000)
        {
            var inputStr = input.ReadToEnd();
            return _stringToObjectTranslator.BuildObject<TObject>(inputStr, rootLabel, maxRewriteIterations);
        }

        public void Serialize<TObject>(TObject inputObject, StreamWriter outputStream, string rootLabel = "root")
        {
            ITrlParseResult output = _objectToAstTranslator.BuildAst(inputObject, rootLabel);
            output.WriteToStream(outputStream);
        }
    }
}
