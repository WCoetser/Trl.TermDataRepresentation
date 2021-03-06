﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Trl.PegParser;
using Trl.PegParser.Grammer;
using Trl.PegParser.Tokenization;
using Trl.TermDataRepresentation.Parser.AST;
using Trl.TermDataRepresentation.Parser.SemanticValidations;

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
                _pegFacade.Token(TokenNames.Comment, new Regex(@"//.*\n")),
                _pegFacade.Token(TokenNames.String, new Regex(@"""(?:(?:\\\"")|[^\""])*?\""", RegexOptions.Compiled)), // \" is used to escape quote characters
                _pegFacade.Token(TokenNames.Variable, new Regex(@"\:[_a-zA-Z]\w*(?:\.[_a-zA-Z]\w*)*", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.Identifier, new Regex(@"[_a-zA-Z]\w*(?:\.[_a-zA-Z]\w*)*", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.Whitespace, new Regex(@"\s+", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.SemiColon, new Regex(@";", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.Number, new Regex(@"[+-]?\d+\.?\d*", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.Comma, new Regex(@",", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.Colon, new Regex(@":", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.OpenRoundBracket, new Regex(@"\(", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.CloseRoundBracket, new Regex(@"\)", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.Arrow, new Regex(@"\=\>", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.OpenAngleBracket, new Regex(@"\<", RegexOptions.Compiled)),
                _pegFacade.Token(TokenNames.CloseAngleBracket, new Regex(@"\>", RegexOptions.Compiled))                
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

            _pegFacade.DefaultSemanticActions.SetTerminalAction(TokenNames.Variable,
                (matchedTokens, _, pegSpec) => new Variable { Name = matchedTokens.GetMatchedString() });

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.Statement, (_, subResults, pegSpec) => subResults.First());

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.Term,
                (_, subResults, pegSpec) => subResults.First());

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.Start,
                (_, subResults, pegSpec) =>
                {
                    var oneOrMore = subResults.Cast<GenericResult>().Single();
                    StatementList statementList = new StatementList 
                    { 
                        Statements = new List<TermStatement>(),
                        RewriteRules = new List<RewriteRule>()
                    };
                    foreach (var nestedBrackets in oneOrMore.SubResults.Cast<GenericResult>())
                    {
                        // Rewrite rule statements
                        var rewriteRule = nestedBrackets.SubResults[0] as RewriteRule;
                        if (rewriteRule != null)
                        {
                            statementList.RewriteRules.Add(rewriteRule);
                            continue;
                        }

                        // Term statements
                        var sequence = (GenericResult)nestedBrackets.SubResults[0];
                        if (sequence != null) // empty statement generated by ";;;" type input
                        {
                            var label = sequence.SubResults[0];
                            var term = sequence.SubResults.Skip(1).First();
                            if (term != null) // optional case: where we have a string of semi-colons
                            {
                                statementList.Statements.Add(new TermStatement
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

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.TermList, (matchTokens, subResults, matchedSpec) =>
            {
                var listResult = subResults.First().GetSubResults()[1]; // skip over "("
                var cst = (CommaSeperatedTerms)listResult.GetSubResults()[0];
                return new TermList
                {
                    Terms = cst.Terms
                };
            });

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.CommaSeperatedTerms, (matchTokens, subResults, matchedSpec) =>
            {
                var termList = new CommaSeperatedTerms
                {
                    Terms = new List<ITrlTerm>()
                };
                var results = subResults?.FirstOrDefault()?.GetSubResults();
                var head = results?[0];
                if (head != null)
                {
                    termList.Terms.Add((ITrlTerm)head);
                    // Skip over commas
                    var tail = results[1].GetSubResults();
                    foreach (var tailResult in tail)
                    {
                        var term = tailResult.GetSubResults()[1];
                        termList.Terms.Add((ITrlTerm)term);
                    }                    
                }
                return termList;
            });

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.ClassMemberMapping, (matchTokens, subResults, matchedSpec) =>
            {
                var mappings = new ClassMemberMappingsList
                {
                    ClassMembers = new List<Identifier>()
                };
                var results = subResults?.FirstOrDefault()?.GetSubResults();
                if (results?[1].GetSubResults()[0] != null)
                {
                    var list = results?[1].GetSubResults()[0].GetSubResults();
                    if (list[0] != null)
                    {
                        mappings.ClassMembers.Add((Identifier)list[0]);
                        var tail = list[1].GetSubResults();
                        foreach (var tailResult in tail)
                        {
                            var id = tailResult.GetSubResults()[1];
                            mappings.ClassMembers.Add((Identifier)id);
                        }
                    }
                }
                return mappings;
            });

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.NonACTerm, (matchTokens, subResults, matchedSpec) =>
            {
                var term = new NonAcTerm();
                var concatResults = subResults?.FirstOrDefault()?.GetSubResults();
                term.TermName = (Identifier)concatResults[0];
                var tail = concatResults[1].GetSubResults();
                if (tail.Count == 2)
                {
                    term.ClassMemberMappings = (ClassMemberMappingsList)tail[0];
                    term.Arguments = ((CommaSeperatedTerms)tail[1].GetSubResults()[1].GetSubResults()[0]).Terms;
                }
                else if (tail.Count == 1) 
                {
                    term.Arguments = ((CommaSeperatedTerms)tail[0]).Terms;
                }
                else
                {
                    throw new Exception(); // there could only be 1 or 2 results
                }
                return term;
            });

            _pegFacade.DefaultSemanticActions.SetNonTerminalAction(ParseRuleNames.RewriteRule, (matchTokens, subResults, matchedSpec) =>
            {
                return new RewriteRule
                {
                    MatchTerm = (ITrlTerm)subResults.First().GetSubResults().First(),
                    SubstituteTerm = (ITrlTerm)subResults.First().GetSubResults()[1].GetSubResults()[1]
                };
            });
        }

        private void CreateParser()
        {
            const string grammer = @"
Start => (Statement? [SemiColon])+;
Statement => RewriteRule | Label? Term;
Term => NonACTerm | [Variable] | [Identifier] | [String] | [Number] | TermList;
Label => [Identifier] ([Comma] [Identifier])* [Colon];
TermList => [OpenRoundBracket] CommaSeperatedTerms [CloseRoundBracket];
CommaSeperatedTerms => (Term ([Comma] Term)*)?;
NonACTerm => [Identifier] ClassMemberMapping? [OpenRoundBracket] CommaSeperatedTerms [CloseRoundBracket];
ClassMemberMapping => [OpenAngleBracket] ([Identifier] ([Comma] [Identifier])*)? [CloseAngleBracket];
RewriteRule => Term [Arrow] Term;
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
            var tokensNoWhitespace = tokenizationResult.MatchedRanges.Where(token => token.TokenName != TokenNames.Whitespace 
                && token.TokenName != TokenNames.Comment);
            var parseResult = _parser.Parse(tokensNoWhitespace.ToList().AsReadOnly());
            var validator = new SemanticValidator();
            var semanticErrors = validator.GetSemanticErrors(parseResult);
            if (!parseResult.Succeed || semanticErrors.Count > 0)
            {
                return new TrlParseResult 
                {
                    Succeed = false,
                    Errors = semanticErrors
                };
            }
            else
            {
                return new TrlParseResult 
                {
                    Succeed = true, 
                    Statements = (StatementList)parseResult.SemanticActionResult 
                };
            }
        }
    }
}
