using System;
using System.Collections.Generic;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.Serialization.Translator
{
    /// <summary>
    /// Translates .NET objects into TRL parse results and back for serialization and deserialization.
    /// </summary>
    internal class ObjectTranslator
    {
        internal ITrlParseResult BuildAst<TObject>(TObject inputObject, string rootLabel)
            => inputObject switch
                {
                    string inputString => ConvertString(inputString, rootLabel),
                    _ => throw new NotImplementedException()
                };

        private ITrlParseResult ConvertString(string inputObject, string rootLabel) 
            => new Statement
            {
                Term = new StringValue
                {
                    Value = Convert.ToString(inputObject)
                },
                Label = new Label
                {
                    Identifiers = new List<Identifier>
                            {
                                new Identifier
                                {
                                    Name = rootLabel
                                }
                            }
                }
            };
    }
}
