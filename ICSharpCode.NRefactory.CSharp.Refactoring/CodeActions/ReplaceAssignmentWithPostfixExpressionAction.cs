//
// ReplaceAssignmentWithPostfixExpressionAction.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Replace assignment with postfix expression")]
	[ExportCodeRefactoringProvider("Replace assignment with postfix expression", LanguageNames.CSharp)]
	public class ReplaceAssignmentWithPostfixExpressionAction : ICodeRefactoringProvider
	{
//		static readonly AstNode onePattern = PatternHelper.OptionalParentheses(new PrimitiveExpression (1));
//
        public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken);
            var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
            var token = root.FindToken(span.Start);
            
            var node = token.Parent as BinaryExpressionSyntax;
            if(node == null)
                return Enumerable.Empty<CodeAction>();

            var updatedNode = ReplaceWithOperatorAssignmentAction.CreateAssignment(node) ?? node;

            if ((!updatedNode.OperatorToken.IsKind(SyntaxKind.PlusEqualsToken) && !updatedNode.OperatorToken.IsKind(SyntaxKind.MinusEqualsToken)))
                return Enumerable.Empty<CodeAction>();

            var rightLiteral = updatedNode.Right as LiteralExpressionSyntax;
            if (rightLiteral == null || ((int)rightLiteral.Token.Value) != 1)
                return Enumerable.Empty<CodeAction>();

            String desc = updatedNode.OperatorToken.IsKind(SyntaxKind.PlusEqualsToken) ? "Replace with '{0}++'" : "Replace with '{0}--'";

            var newNode = SyntaxFactory.PostfixUnaryExpression(updatedNode.OperatorToken.IsKind(SyntaxKind.PlusEqualsToken) ? SyntaxKind.PostIncrementExpression :
                SyntaxKind.PostDecrementExpression, updatedNode.Left).WithAdditionalAnnotations(Formatter.Annotation);
            return new [] { CodeActionFactory.Create(span, DiagnosticSeverity.Info, String.Format(desc, node.Left.ToString()), document.WithSyntaxRoot(
                root.ReplaceNode(node as ExpressionSyntax, newNode)))};
        }

//		protected override CodeAction GetAction(SemanticModel context, AssignmentExpression node)
//		{
//			if (!node.OperatorToken.Contains(context.Location))
//				return null;
//			node = ReplaceWithOperatorAssignmentAction.CreateAssignment(node) ?? node;
//			if (node.Operator != AssignmentOperatorType.Add && node.Operator != AssignmentOperatorType.Subtract || !onePattern.IsMatch (node.Right))
//				return null;
//			string desc = node.Operator == AssignmentOperatorType.Add ? context.TranslateString("Replace with '{0}++'") : context.TranslateString("Replace with '{0}--'");
//			return new CodeAction(
//				string.Format(desc, node.Left),
//				s => s.Replace(node, new UnaryOperatorExpression(
//					node.Operator == AssignmentOperatorType.Add ? UnaryOperatorType.PostIncrement : UnaryOperatorType.PostDecrement,
//					node.Left.Clone()
//				)),
//				node.OperatorToken
//			);
//		}
    }
}

