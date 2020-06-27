﻿using System;
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
                _pegFacade.Token(TokenNames.Number, new Regex(@"[+-]?\d+\.?\d*", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.Comma, new Regex(@",", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.Colon, new Regex(@":", RegexOptions.Compiled))
            });
        }

        private void CreateSemanticActions()
        {
            var replaceQuotesRegex = new Regex("(^\")|(\"$)", RegexOptions.Compiled);

            _pegFacade.DefaultSemanticActions.SetDefaultGenericPassthroughAction<GenericResult>();

            _pegFacade.DefaultSemanticActions.OrderedChoiceAction = (_, subResults, pegSpec) => subResults.First();

            _pegFacade.DefaultSemanticActions.OptionalAction = (_, subResults, pegSpec) => subResults.FirstOrDefault();

            _pegFacade.DefaultSemanticActions.SetTerminalAction(TokenNames.Identifier, 
                (matchedTokens, _, pegSpec) => new Identifier { Name = matchedTokens.GetMatchedString() });

            _pegFacade.DefaultSemanticActions.SetTerminalAction(TokenNames.String,
                (matchedTokens, _, pegSpec) => new StringValue { Value = replaceQuotesRegex.Replace(matchedTokens.GetMatchedString(), string.Empty) });

            _pegFacade.DefaultSemanticActions.SetTerminalAction(TokenNames.Number,
                (matchedTokens, _, pegSpec) => new NumericValue { Value = matchedTokens.GetMatchedString() });

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.Statement,
                (_, subResults, pegSpec) => subResults.First());

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.Term,
                (_, subResults, pegSpec) => subResults.First());

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.Start,
                (_, subResults, pegSpec) =>
                {
                    var oneOrMore = subResults.Cast<GenericResult>().Single();
                    StatementList statementList = new StatementList { Statements = new List<Statement>() };
                    foreach (var nestedBrackets in oneOrMore.SubResults.Cast<GenericResult>())
                    {
                        var sequence = (GenericResult)nestedBrackets.SubResults[0];
                        if (sequence != null) // empty statement generated by ";;;" type input
                        {
                            var label = sequence.SubResults[0];
                            var term = sequence.SubResults.Skip(1).First();
                            if (term != null) // optional case: where we have a string of semi-colons
                            {
                                statementList.Statements.Add(new Statement
                                {
                                    Label = (Label)label,
                                    Term = (ITrlTerm)term
                                });
                            }
                        }
                    }
                    return statementList;
                });

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.Label, (matchTokens, subResults, matchedSpec) =>
            {
                var returnLabel = new Label
                {
                    Identifiers = new List<Identifier>()
                };
                var concatResults = subResults.First().GetSubResults();
                var identifierHead = (Identifier)concatResults[0];
                returnLabel.Identifiers.Add(identifierHead);
                var starResults = concatResults[1].GetSubResults()[0];
                foreach (var nextIdentifier in starResults.GetSubResults()) {
                    returnLabel.Identifiers.Add((Identifier)nextIdentifier.GetSubResults()[1]);
                }
                return returnLabel;
            });
        }

        private void CreateParser()
        {
            const string grammer = @"
Start => (Statement? [SemiColon])+;
Statement => Label? Term;
Term => [Identifier] | [String] | [Number];
Label => [Identifier] ([Comma] [Identifier])* [Colon]
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
                    Statements = (StatementList)parseResult.SemanticActionResult 
                };
            }
        }
    }
}
