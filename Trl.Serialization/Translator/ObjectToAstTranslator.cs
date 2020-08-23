using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.Serialization.Translator
{
    internal class ObjectToAstTranslator
    {
        public const BindingFlags Bindings = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

        internal ITrlParseResult BuildAst<TObject>(TObject inputObject, string rootLabel)
        {
            ITrlParseResult expression = BuildAstForObject(inputObject);

            return new TermStatement
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

        private ITrlTerm BuildAstForObject(object inputObject)
        {
            if (inputObject == null)
            {
                return new Identifier
                {
                    Name = "null"
                };
            }
            else if (inputObject is string)
            {
                return new StringValue
                {
                    Value = Convert.ToString(inputObject)
                };
            }
            // NB: IEnumerable must be after string because string is IEnumerable
            else if (inputObject is IEnumerable)
            {
                var list = new TermList()
                {
                    Terms = new List<ITrlTerm>()
                };
                var inputEnumerable = (IEnumerable)inputObject;
                foreach (var item in inputEnumerable)
                {
                    list.Terms.Add((ITrlTerm)BuildAstForObject(item));
                }
                return list;
            }
            else if (IsNumeric(inputObject))
            {
                return new NumericValue
                {
                    Value = Convert.ToString(inputObject)
                };
            }
            else
            {
                return GenerateNonAcTerm(inputObject);
            }
        }

        private ITrlTerm GenerateNonAcTerm(object inputObject)
        {
            // Assume we are creating a non ac term in the default case
            var type = inputObject.GetType();
            var properties = type.GetProperties(Bindings)
                                .Where(p => p.CanRead).OrderBy(p => p.Name);
            var fields = type.GetFields(Bindings)
                                .OrderBy(p => p.Name);

            // Build arguments first
            var fieldMappingNames = new List<string>();
            var arguments = new List<ITrlTerm>();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(inputObject);
                if (value != null)
                {
                    fieldMappingNames.Add(prop.Name);
                    arguments.Add(BuildAstForObject(value));
                }
            }
            foreach (var field in fields)
            {
                var value = field.GetValue(inputObject);
                if (value != null)
                {
                    fieldMappingNames.Add(field.Name);
                    arguments.Add(BuildAstForObject(value));
                }
            }

            return new NonAcTerm
            {
                TermName = new Identifier
                {
                    Name = type.Name
                },
                ClassMemberMappings = new ClassMemberMappingsList
                {
                    ClassMembers = fieldMappingNames.Select(name => new Identifier { Name = name }).ToList(),
                },
                Arguments = arguments
            };
        }

        private static bool IsNumeric(object inputObject)
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
    }
}
