using System;
using Trl.IntegerMapper;
using Trl.IntegerMapper.EqualityComparerIntegerMapper;
using Trl.IntegerMapper.StringIntegerMapper;
using Trl.TermDataRepresentation.Parser;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.TermDataRepresentation.Database
{
    /// <summary>
    /// Main storage for terms.
    /// </summary>    
    public class TermDatabase
    {
        private readonly IIntegerMapper<string> _stringValueMapper;
        private readonly IIntegerMapper<ConstantSymbol> _constantSymbolMapper;

        public TermDatabase()
        {
            _stringValueMapper = new StringMapper();
            _constantSymbolMapper = new EqualityComparerMapper<ConstantSymbol>(new ConstantSymbolEqualityComparer());
        }

        /// <summary>
        /// Reconstructs a term from an identifier.
        /// </summary>
        public ITrlTerm ReadTerm(ulong termIdentifier)
        {
            var constantSymbol =_constantSymbolMapper.ReverseMap(termIdentifier);
            return constantSymbol.Type switch
            {
                ConstantSymbolType.String => new StringValue { Value = _stringValueMapper.ReverseMap(constantSymbol.Value) },
                ConstantSymbolType.Identifier => new Identifier { Name = _stringValueMapper.ReverseMap(constantSymbol.Value) },
                ConstantSymbolType.Number => new NumericValue { Value = _stringValueMapper.ReverseMap(constantSymbol.Value) },
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// Saves an AST term and returns an identifier.
        /// </summary>
        public ulong SaveTerm(ITrlTerm parseResult)
        {
            ulong mappedValue = 
                parseResult switch
                {
                    Identifier id => _stringValueMapper.Map(id.Name),
                    StringValue str => _stringValueMapper.Map(str.Value),
                    NumericValue num => _stringValueMapper.Map(num.Value),
                    _ => throw new NotImplementedException()
                };

            var symbolType =
                parseResult switch
                {
                    Identifier _ => ConstantSymbolType.Identifier,
                    StringValue _ => ConstantSymbolType.String,
                    NumericValue _ => ConstantSymbolType.Number,
                    _ => throw new NotImplementedException()
                };

            return _constantSymbolMapper.Map(new ConstantSymbol(mappedValue, symbolType));            
        }
    }
}
