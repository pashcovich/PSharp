﻿//-----------------------------------------------------------------------
// <copyright file="LockStatementNode.cs">
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

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Lock statement node.
    /// </summary>
    internal sealed class LockStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The lock keyword.
        /// </summary>
        internal Token LockKeyword;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        internal Token LeftParenthesisToken;

        /// <summary>
        /// The lock.
        /// </summary>
        internal ExpressionNode Lock;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        internal Token RightParenthesisToken;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal StatementBlockNode StatementBlock;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="node">Node</param>
        internal LockStatementNode(IPSharpProgram program, StatementBlockNode node)
            : base(program, node)
        {

        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite()
        {
            this.Lock.Rewrite();
            this.StatementBlock.Rewrite();

            var text = this.GetRewrittenLockStatement();

            base.TextUnit = new TextUnit(text, this.LockKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            this.Lock.Model();
            this.StatementBlock.Model();

            var text = this.GetRewrittenLockStatement();

            base.TextUnit = new TextUnit(text, this.LockKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten lock statement.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenLockStatement()
        {
            var text = "";

            text += this.LockKeyword.TextUnit.Text;

            text += this.LeftParenthesisToken.TextUnit.Text;

            text += this.Lock.TextUnit.Text;

            text += this.RightParenthesisToken.TextUnit.Text;

            text += this.StatementBlock.TextUnit.Text;

            return text;
        }

        #endregion
    }
}