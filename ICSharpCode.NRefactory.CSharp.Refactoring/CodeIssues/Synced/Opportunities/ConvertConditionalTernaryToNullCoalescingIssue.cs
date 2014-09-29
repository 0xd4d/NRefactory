// 
// ConditionalToNullCoalescingInspector.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	/// <summary>
	/// Checks for "a != null ? a : other"<expr>
	/// Converts to: "a ?? other"<expr>
	/// </summary>
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ConvertConditionalTernaryToNullCoalescing")]
	public class ConvertConditionalTernaryToNullCoalescingIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ConvertConditionalTernaryToNullCoalescingIssue";
		const string Description            = "'?:' expression can be converted to '??' expression.";
		const string MessageFormat          = "'?:' expression can be re-written as '??' expression";
		const string Category               = IssueCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "'?:' expression can be converted to '??' expression");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ConvertConditionalTernaryToNullCoalescingIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			static ExpressionSyntax AnalyzeBinaryExpression (ExpressionSyntax node)
			{
				var bOp = node.SkipParens() as BinaryExpressionSyntax;
				if (bOp == null)
					return null;
				if (bOp.IsKind(SyntaxKind.NotEqualsExpression) || bOp.IsKind(SyntaxKind.EqualsExpression)) {
					if (bOp.Left != null && bOp.Left.SkipParens().IsKind(SyntaxKind.NullLiteralExpression))
						return bOp.Right;
					if (bOp.Right != null && bOp.Right.SkipParens().IsKind(SyntaxKind.NullLiteralExpression))
						return bOp.Left;
				}
				return null;
			}

			public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
			{
				base.VisitConditionalExpression(node);
				var obj = AnalyzeBinaryExpression(node.Condition);
				if (obj == null)
					return;
				if (node.Condition.SkipParens().IsKind(SyntaxKind.NotEqualsExpression)) {
					if (obj.SkipParens().IsEquivalentTo(node.WhenTrue.SkipParens(), true)) {
						AddIssue(Diagnostic.Create(Rule, node.GetLocation()));
						return;
					}
					var cast = node.WhenTrue as CastExpressionSyntax;
					if (cast != null && cast.Expression != null && obj.SkipParens().IsEquivalentTo(cast.Expression.SkipParens(), true)) {
						AddIssue(Diagnostic.Create(Rule, node.GetLocation()));
						return;
					}
				} else {
					if (obj.SkipParens().IsEquivalentTo(node.WhenFalse.SkipParens(), true)) {
						AddIssue(Diagnostic.Create(Rule, node.GetLocation()));
						return;
					}
				}
			}
		}
	}

	[ExportCodeFixProvider(ConvertConditionalTernaryToNullCoalescingIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ConvertConditionalTernaryToNullCoalescingFixProvider : NRefactoryCodeFixProvider
	{
		public override IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return ConvertConditionalTernaryToNullCoalescingIssue.DiagnosticId;
		}

		public override async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan) as ConditionalExpressionSyntax;
				if (node == null)
					continue;
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, "Replace '?:'  operator with '??", token => {
					ExpressionSyntax a, other;
					if (node.Condition.SkipParens().IsKind(SyntaxKind.EqualsExpression)) {
						a = node.WhenFalse;
						other = node.WhenTrue;
					} else {
						other = node.WhenFalse;
						a = node.WhenTrue;
					}

					if (node.Condition.SkipParens().IsKind(SyntaxKind.EqualsExpression)) {
						var castExpression = other as CastExpressionSyntax;
						if (castExpression != null) {
							a = SyntaxFactory.CastExpression(castExpression.Type, a);
							other = castExpression.Expression;
						}
					}

					ExpressionSyntax newNode = SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, a, other);

					var newRoot = root.ReplaceNode(node, newNode.WithLeadingTrivia(node.GetLeadingTrivia()).WithAdditionalAnnotations(Formatter.Annotation));
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
 				}));
			}
			return result;
		}
	}
}