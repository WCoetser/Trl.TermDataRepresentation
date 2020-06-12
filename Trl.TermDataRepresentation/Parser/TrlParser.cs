using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Trl.PegParser;
using Trl.PegParser.Grammer;
using Trl.PegParser.Tokenization;
using Trl.TermDataRepresentation.Parser.AST;

namespace Trl.TermDataRepresentation.Parser
{
    public class TrlParser
    {
        private readonly PegFacade<TokenNames, ParseRuleNames, ITrlParseResult> _pegFacade;
        private Tokenizer<TokenNames> _tokenizer;
        private Parser<TokenNames, ParseRuleNames, ITrlParseResult> _parser;

        public TrlParser()
        {
            _pegFacade = new PegFacade<TokenNames, ParseRuleNames, ITrlParseResult>();
            CreateTokenizer();
            CreateSemanticActions();
            CreateParser();
        }

        private void CreateTokenizer()
        {
            // NB: This list of token definitions is prioritized
            _tokenizer = _pegFacade.Tokenizer(new[] {
                _pegFacade.Token(TokenNames.String, new Regex("\"(?:[^\"]|(?:\\\"))*\"", RegexOptions.Compiled)), // \" is used to escape quote characters
                _pegFacade.Token(TokenNames.Identifier, new Regex(@"[_a-zA-Z]\w*(?:\.[_a-zA-Z]\w*)*", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.Whitespace, new Regex(@"\s+", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.SemiColon, new Regex(@";", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.Number, new Regex(@"[+-]?\d+\.?\d*", RegexOptions.Compiled))
            });
        }

        private void CreateSemanticActions()
        {
            var replaceQuotesRegex = new Regex("(^\")|(\"$)", RegexOptions.Compiled);

            _pegFacade.DefaultSemanticActions.SetDefaultGenericPassthroughAction<GenericResult>();

            _pegFacade.DefaultSemanticActions.OrderedChoiceAction = (_, subResults) => subResults.First();

            _pegFacade.DefaultSemanticActions.SetTerminalAction(TokenNames.Identifier, 
                (matchedTokens, _) => new Identifier { Name = matchedTokens.GetMatchedString() });

            _pegFacade.DefaultSemanticActions.SetTerminalAction(TokenNames.String,
                (matchedTokens, _) => new StringValue { Value = replaceQuotesRegex.Replace(matchedTokens.GetMatchedString(), string.Empty) });

            _pegFacade.DefaultSemanticActions.SetTerminalAction(TokenNames.Number,
                (matchedTokens, _) => new NumericValue { Value = matchedTokens.GetMatchedString() });

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.Statement,
                (_, subResults) => subResults.First());

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.Term,
                (_, subResults) => subResults.First());

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.Start,
                (_, subResults) =>
                {
                    Statements statements = new Statements { StatementList = new List<ITrlParseResult>() };
                    foreach (var nestedBrackets in subResults.Cast<GenericResult>())
                    {
                        var statement = nestedBrackets.SubResults.Cast<GenericResult>().Single();
                        statements.StatementList.Add(statement.SubResults.First()); // this is the part before the semi-colon
                    }
                    return statements;
                });
        }

        private void CreateParser()
        {
            const string grammer = @"
Start => (Statement [SemiColon])+;
Statement => Term;
Term => [Identifier] | [String] | [Number];
";            
            _parser = _pegFacade.Parser(ParseRuleNames.Start, _pegFacade.ParserGenerator.GetParsingRules(grammer));
        }

        public TrlParseResult ParseToAst(string input)
        {
            var tokenizationResult = _tokenizer.Tokenize(input);
            if (!tokenizationResult.Succeed)
            {
                return new TrlParseResult { Succeed = false };
            }
            var tokensNoWhitespace = tokenizationResult.MatchedRanges.Where(token => token.TokenName != TokenNames.Whitespace);
            var parseResult = _parser.Parse(tokensNoWhitespace.ToList().AsReadOnly());
            if (!parseResult.Succeed)
            {
                return new TrlParseResult { Succeed = false };
            }
            else
            {
                return new TrlParseResult { 
                    Succeed = true, 
                    Statements = (Statements)parseResult.SemanticActionResult 
                };
            }
        }
    }
}
