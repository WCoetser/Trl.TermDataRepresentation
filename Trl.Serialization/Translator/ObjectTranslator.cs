using System;
using System.Collections.Generic;
using System.Numerics;
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
        {
            ITrlParseResult expression = (inputObject, IsNumeric(inputObject)) switch
            {
                (string inputString, _) => ConvertString(inputString),
                (_, true) => ConvertNumber(inputObject),
                _ => throw new NotImplementedException()
            };

            return new Statement
            {
                Term = (ITrlTerm)expression,
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

        private ITrlParseResult ConvertNumber<TObject>(TObject inputObject)
            => new NumericValue
            {
                Value = Convert.ToString(inputObject)
            };

        private static bool IsNumeric<TObject>(TObject inputObject) 
            => inputObject is sbyte
            || inputObject is byte
            || inputObject is short
            || inputObject is ushort
            || inputObject is int
            || inputObject is uint
            || inputObject is long
            || inputObject is ulong
            || inputObject is BigInteger
            || inputObject is decimal
            || inputObject is float
            || inputObject is double;

        private ITrlParseResult ConvertString(string inputObject)
            => new StringValue
            {
                Value = Convert.ToString(inputObject)
            };
            
    }
}
