﻿//-----------------------------------------------------------------------
// <copyright file="MachineDeclarationVisitor.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.Parsing.Syntax;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# machine declaration parsing visitor.
    /// </summary>
    internal sealed class MachineDeclarationVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal MachineDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="parentNode">Node</param>
        /// <param name="isMain">Is main machine</param>
        /// <param name="isModel">Is a model</param>
        /// <param name="isMonitor">Is a monitor</param>
        /// <param name="accMod">Access modifier</param>
        /// <param name="inhMod">Inheritance modifier</param>
        internal void Visit(IPSharpProgram program, NamespaceDeclarationNode parentNode, bool isMain,
            bool isModel, bool isMonitor, AccessModifier accMod, InheritanceModifier inhMod)
        {
            var node = new MachineDeclarationNode(base.TokenStream.Program, isMain, isModel, isMonitor);
            node.AccessModifier = accMod;
            node.InheritanceModifier = inhMod;
            node.MachineKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected machine identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.TokenStream.CurrentMachine = base.TokenStream.Peek().Text;
            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.MachineIdentifier));

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Program is PSharpProgram)
            {
                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.Colon &&
                    base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                {
                    throw new ParsingException("Expected \":\" or \"{\".",
                        new List<TokenType>
                    {
                            TokenType.Colon,
                            TokenType.LeftCurlyBracket
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.Colon)
                {
                    node.ColonToken = base.TokenStream.Peek();

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    if (base.TokenStream.Done ||
                        base.TokenStream.Peek().Type != TokenType.Identifier)
                    {
                        throw new ParsingException("Expected base machine identifier.",
                            new List<TokenType>
                        {
                                TokenType.Identifier
                        });
                    }

                    while (!base.TokenStream.Done &&
                        base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
                    {
                        if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                            base.TokenStream.Peek().Type != TokenType.Dot &&
                            base.TokenStream.Peek().Type != TokenType.NewLine)
                        {
                            throw new ParsingException("Expected base machine identifier.",
                                new List<TokenType>
                            {
                                    TokenType.Identifier,
                                    TokenType.Dot
                            });
                        }
                        else
                        {
                            node.BaseNameTokens.Add(base.TokenStream.Peek());
                        }

                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }
                }
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                throw new ParsingException("Expected \"{\".",
                    new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.MachineLeftCurlyBracket));

            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Program is PSharpProgram)
            {
                this.VisitNextPSharpIntraMachineDeclaration(node);
                parentNode.MachineDeclarations.Add(node);
            }
            else
            {
                this.VisitNextPIntraMachineDeclaration(node);
                (program as PProgram).MachineDeclarations.Add(node);
            }

            if (node.StateDeclarations.Count == 0)
            {
                throw new ParsingException("A machine must declare at least one state.",
                    new List<TokenType>());
            }

            var initialStates = node.StateDeclarations.FindAll(s => s.IsInitial);
            if (initialStates.Count == 0)
            {
                throw new ParsingException("A machine must declare a start state.",
                    new List<TokenType>());
            }
            else if (initialStates.Count > 1)
            {
                throw new ParsingException("A machine can declare only a single start state.",
                    new List<TokenType>());
            }
        }

        /// <summary>
        /// Visits the next intra-machine declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextPSharpIntraMachineDeclaration(MachineDeclarationNode node)
        {
            if (base.TokenStream.Done)
            {
                throw new ParsingException("Expected \"}\".",
                    new List<TokenType>
                {
                    TokenType.Private,
                    TokenType.Protected,
                    TokenType.StartState,
                    TokenType.StateDecl,
                    TokenType.ModelDecl,
                    TokenType.LeftSquareBracket,
                    TokenType.RightCurlyBracket
                });
            }

            bool fixpoint = false;
            var token = base.TokenStream.Peek();
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    base.TokenStream.Index++;
                    break;

                case TokenType.CommentLine:
                case TokenType.Region:
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.StartState:
                case TokenType.StateDecl:
                case TokenType.ModelDecl:
                case TokenType.Void:
                case TokenType.Object:
                case TokenType.MachineDecl:
                case TokenType.Int:
                case TokenType.Float:
                case TokenType.Double:
                case TokenType.Bool:
                case TokenType.Identifier:
                case TokenType.Private:
                case TokenType.Protected:
                case TokenType.Internal:
                case TokenType.Public:
                    this.VisitMachineLevelDeclaration(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    new AttributeListVisitor(base.TokenStream).Visit();
                    base.TokenStream.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.MachineRightCurlyBracket));
                    node.RightCurlyBracketToken = base.TokenStream.Peek();
                    base.TokenStream.CurrentMachine = "";
                    fixpoint = true;
                    base.TokenStream.Index++;
                    break;

                default:
                    throw new ParsingException("Unexpected token.",
                        new List<TokenType>());
            }

            if (!fixpoint)
            {
                this.VisitNextPSharpIntraMachineDeclaration(node);
            }
        }

        /// <summary>
        /// Visits a machine level declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitMachineLevelDeclaration(MachineDeclarationNode parentNode)
        {
            AccessModifier am = AccessModifier.None;
            InheritanceModifier im = InheritanceModifier.None;
            bool isStart = false;
            bool isModel = false;

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.StateDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Void &&
                base.TokenStream.Peek().Type != TokenType.Object &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Float &&
                base.TokenStream.Peek().Type != TokenType.Double &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                if (am != AccessModifier.None &&
                    (base.TokenStream.Peek().Type == TokenType.Public ||
                    base.TokenStream.Peek().Type == TokenType.Private ||
                    base.TokenStream.Peek().Type == TokenType.Protected ||
                    base.TokenStream.Peek().Type == TokenType.Internal))
                {
                    throw new ParsingException("More than one protection modifier.",
                        new List<TokenType>());
                }
                else if (im != InheritanceModifier.None &&
                    base.TokenStream.Peek().Type == TokenType.Abstract)
                {
                    throw new ParsingException("Duplicate abstract modifier.",
                        new List<TokenType>());
                }
                else if (isStart &&
                    base.TokenStream.Peek().Type == TokenType.StartState)
                {
                    throw new ParsingException("Duplicate start state modifier.",
                        new List<TokenType>());
                }
                else if (isModel &&
                    base.TokenStream.Peek().Type == TokenType.ModelDecl)
                {
                    throw new ParsingException("Duplicate model method modifier.",
                        new List<TokenType>());
                }

                if (base.TokenStream.Peek().Type == TokenType.Public)
                {
                    am = AccessModifier.Public;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Private)
                {
                    am = AccessModifier.Private;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Protected)
                {
                    am = AccessModifier.Protected;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Internal)
                {
                    am = AccessModifier.Internal;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Abstract)
                {
                    im = InheritanceModifier.Abstract;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Virtual)
                {
                    im = InheritanceModifier.Virtual;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Override)
                {
                    im = InheritanceModifier.Override;
                }
                else if (base.TokenStream.Peek().Type == TokenType.StartState)
                {
                    isStart = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.ModelDecl)
                {
                    isModel = true;
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.StateDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Void &&
                base.TokenStream.Peek().Type != TokenType.Object &&
                base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Float &&
                base.TokenStream.Peek().Type != TokenType.Double &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Identifier))
            {
                throw new ParsingException("Expected state or method declaration.",
                    new List<TokenType>
                {
                    TokenType.StateDecl,
                    TokenType.MachineDecl,
                    TokenType.Void,
                    TokenType.Object,
                    TokenType.Int,
                    TokenType.Float,
                    TokenType.Double,
                    TokenType.Bool,
                    TokenType.Identifier
                });
            }
            
            if (base.TokenStream.Peek().Type == TokenType.StateDecl)
            {
                if (am == AccessModifier.Public)
                {
                    throw new ParsingException("A state cannot be public.",
                        new List<TokenType>());
                }
                else if (am == AccessModifier.Internal)
                {
                    throw new ParsingException("A state cannot be internal.",
                        new List<TokenType>());
                }

                if (im == InheritanceModifier.Abstract)
                {
                    throw new ParsingException("A state cannot be abstract.",
                        new List<TokenType>());
                }
                else if (im == InheritanceModifier.Virtual)
                {
                    throw new ParsingException("A state cannot be virtual.",
                        new List<TokenType>());
                }
                else if (im == InheritanceModifier.Override)
                {
                    throw new ParsingException("A state cannot be overriden.",
                        new List<TokenType>());
                }

                if (isModel)
                {
                    throw new ParsingException("A state cannot be a model.",
                        new List<TokenType>());
                }

                new StateDeclarationVisitor(base.TokenStream).Visit(parentNode, isStart, am);
            }
            else
            {
                if (am == AccessModifier.Public)
                {
                    throw new ParsingException("A field or method cannot be public.",
                        new List<TokenType>());
                }
                else if (am == AccessModifier.Internal)
                {
                    throw new ParsingException("A field or method cannot be internal.",
                        new List<TokenType>());
                }

                new FieldOrMethodDeclarationVisitor(base.TokenStream).Visit(parentNode,
                    isModel, am, im);
            }
        }

        /// <summary>
        /// Visits the next intra-machine declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextPIntraMachineDeclaration(MachineDeclarationNode node)
        {
            if (base.TokenStream.Done)
            {
                throw new ParsingException("Expected \"}\".",
                    new List<TokenType>
                {
                    TokenType.StartState,
                    TokenType.StateDecl,
                    TokenType.FunDecl,
                    TokenType.Var
                });
            }

            bool fixpoint = false;
            var token = base.TokenStream.Peek();
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    base.TokenStream.Index++;
                    break;

                case TokenType.CommentLine:
                case TokenType.Region:
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.StartState:
                    this.VisitStartStateModifier(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.StateDecl:
                    new StateDeclarationVisitor(base.TokenStream).Visit(node, false, AccessModifier.None);
                    base.TokenStream.Index++;
                    break;

                case TokenType.ModelDecl:
                    new FunctionDeclarationVisitor(base.TokenStream).Visit(node, true);
                    base.TokenStream.Index++;
                    break;

                case TokenType.FunDecl:
                    new FunctionDeclarationVisitor(base.TokenStream).Visit(node, false);
                    base.TokenStream.Index++;
                    break;

                case TokenType.Var:
                    new FieldDeclarationVisitor(base.TokenStream).Visit(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.ColdState:
                case TokenType.HotState:
                    base.TokenStream.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.MachineRightCurlyBracket));
                    node.RightCurlyBracketToken = base.TokenStream.Peek();
                    base.TokenStream.CurrentMachine = "";
                    fixpoint = true;
                    base.TokenStream.Index++;
                    break;

                default:
                    throw new ParsingException("Unexpected token.",
                        new List<TokenType>());
            }

            if (!fixpoint)
            {
                this.VisitNextPIntraMachineDeclaration(node);
            }
        }

        /// <summary>
        /// Visits a start state modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitStartStateModifier(MachineDeclarationNode parentNode)
        {
            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.StateDecl &&
                base.TokenStream.Peek().Type != TokenType.ColdState &&
                base.TokenStream.Peek().Type != TokenType.HotState))
            {
                throw new ParsingException("Expected state declaration.",
                    new List<TokenType>
                {
                    TokenType.StateDecl
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.ColdState ||
                base.TokenStream.Peek().Type == TokenType.HotState)
            {
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.StateDecl)
                {
                    throw new ParsingException("Expected state declaration.",
                        new List<TokenType>
                    {
                        TokenType.StateDecl
                    });
                }
            }

            new StateDeclarationVisitor(base.TokenStream).Visit(parentNode, true, AccessModifier.None);
        }
    }
}
