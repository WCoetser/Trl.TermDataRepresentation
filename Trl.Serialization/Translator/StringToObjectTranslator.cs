using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Trl.TermDataRepresentation.Database;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.Serialization.Translator
{
    internal class StringToObjectTranslator
    {
        private readonly TrlParser _parser;
        private readonly Dictionary<(ulong, string), object> _objectCache;

        internal StringToObjectTranslator()
        {
            _parser = new TrlParser();
            _objectCache = new Dictionary<(ulong, string), object>(EqualityComparer<ValueTuple<ulong, string>>.Default);
        }

        internal TObject BuildObject<TObject>(string inputString, string rootLabel, int maxRewriteIterations = 100000)
        {
            var database = new TermDatabase();
            var result = _parser.ParseToAst(inputString);
            if (!result.Succeed)
            {
                throw new Exception(string.Join(Environment.NewLine, result.Errors));
            }
            database.Writer.StoreStatements(result.Statements);
            database.ExecuteRewriteRules(maxRewriteIterations);
            var statementList = database.Reader.ReadStatementsForLabel(rootLabel);
            if (statementList == default || statementList.Statements.Count == 0)
            {
                return default;
            }
            if (statementList.Statements.Count != 1)
            {
                throw new Exception("More than one result for given label");
            }
            return (TObject)ConvertToObject(typeof(TObject), statementList.Statements.Single().Term);
        }

        private object ConvertToObject(Type targetType, ITrlTerm term)
        {            
            return term switch
            {
                StringValue str => str.Value,
                NumericValue num => ConvertToNumeric(targetType, num.Value),
                Identifier id => ConvertIdentifier(targetType, id),
                TermList termList => ConvertToCollectionOrArray(targetType, termList),
                NonAcTerm nonAcTerm => ConvertFromAcTerm(targetType, nonAcTerm),
                _ => throw new NotImplementedException()
            };
        }

        private object ConvertFromAcTerm(Type targetType, NonAcTerm nonAcTerm)
        {
            if (nonAcTerm.ClassMemberMappings == null)
            {
                // TODO: Generate class mappings based on object members
                throw new Exception($"Unable to translate {nonAcTerm.ToSourceCode()} to object of type {targetType.FullName}: class member mappings not given.");
            }

            var outputObject = Activator.CreateInstance(targetType);

            int memberCount = nonAcTerm.ClassMemberMappings.ClassMembers.Count;
            for (int i = 0; i < memberCount; i++)
            {
                ITrlTerm arg = nonAcTerm.Arguments[i];
                var memberName = nonAcTerm.ClassMemberMappings.ClassMembers[i].Name;
                try
                {
                    var property = targetType.GetProperty(memberName, ObjectToAstTranslator.Bindings);
                    if (property != null)
                    {
                        var newValue = ConvertToObject(property.PropertyType, arg);
                        property.SetValue(outputObject, newValue);
                        continue;
                    }
                    var field = targetType.GetField(memberName, ObjectToAstTranslator.Bindings);
                    if (field != null)
                    {
                        var newValue = ConvertToObject(field.FieldType, arg);
                        field.SetValue(outputObject, newValue);
                        continue;
                    }
                }
                catch(Exception ex)
                {
                    throw new Exception($"Unable to bind '{arg.ToSourceCode()}' to '{memberName}' on type '{targetType.FullName}', see inner exception for more information.", ex);
                }

                // If it got this far property or field is not found
                throw new Exception($"Unable to find public field or property with name '{memberName}' on type '{targetType.FullName}' for binding '{arg.ToSourceCode()}'");
            }
            return outputObject;
        }

        private object ConvertToCollectionOrArray(Type targetType, TermList termList)
        {
            bool isTargetObject = targetType.IsAssignableFrom(typeof(object));

            // Array case
            if (targetType.IsArray || isTargetObject)
            {
                var arrayElementType = isTargetObject ? typeof(object) : targetType.GetElementType();
                Array outputArray = Array.CreateInstance(arrayElementType, termList.Terms.Count);
                for (int i = 0; i < termList.Terms.Count; i++)
                {
                    outputArray.SetValue(ConvertToObject(arrayElementType, termList.Terms[i]), i);
                }
                return outputArray;
            }

            // ICollection case
            var constructorInfo = targetType.GetConstructor(Array.Empty<Type>());
            var collectionObject = constructorInfo.Invoke(Array.Empty<object>());
            var genericArg = targetType.GenericTypeArguments.Single();
            var addMethod = targetType.GetMethod("Add", BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
            foreach (ITrlTerm subTerm in termList.Terms)
            {
                var collectionItem = ConvertToObject(genericArg, subTerm);
                addMethod.Invoke(collectionObject, new[] { collectionItem });
            }

            return collectionObject;
        }

        private object ConvertIdentifier(Type _, Identifier id)
        {
            return id.Name switch
            {
                "null" => null,
                _ => throw new NotImplementedException()
            };
        }

        internal object ConvertToNumeric(Type targetType, string value)
        {
            if (targetType.IsAssignableFrom(typeof(object)))
            {
                return Convert.ChangeType(value, typeof(decimal));
            }
            return Convert.ChangeType(value, targetType);
        }
    }
}
