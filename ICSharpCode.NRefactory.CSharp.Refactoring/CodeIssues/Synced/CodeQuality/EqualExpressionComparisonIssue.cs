//
// EqualExpressionComparisonIssue.cs
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
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "EqualExpressionComparison")]
	public class EqualExpressionComparisonIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "EqualExpressionComparisonIssue";
		const string Description            = "Comparing equal expression for equality is usually useless";
		const string MessageFormat          = "Equal expression comparison";
		const string Category               = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule1 = new DiagnosticDescriptor (DiagnosticId, Description, "Replace with 'true'", Category, DiagnosticSeverity.Warning, true, "Equal expression comparison");
		static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor (DiagnosticId, Description, "Replace with 'false'", Category, DiagnosticSeverity.Warning, true, "Equal expression comparison");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule1, Rule2);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<EqualExpressionComparisonIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			void AddIssue(AstNode nodeToReplace, AstNode highlightNode, bool replaceWithTrue)
//			{
//				AddIssue(new CodeIssue(
//					highlightNode, 
//					ctx.TranslateString(""), 
//					replaceWithTrue ? ctx.TranslateString() : ctx.TranslateString(), 
//					script =>  {
//						script.Replace(nodeToReplace, new PrimitiveExpression(replaceWithTrue));
//					}
//				));
//			}
//
//
//			readonly BinaryOperatorExpression pattern = 
//				new BinaryOperatorExpression(
//					PatternHelper.OptionalParentheses(new AnyNode("expression")), 
//					BinaryOperatorType.Any, 
//					PatternHelper.OptionalParentheses(new Backreference("expression"))
//				);
//
//			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
//			{
//				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
//
//				if (binaryOperatorExpression.Operator != BinaryOperatorType.Equality &&
//				    binaryOperatorExpression.Operator != BinaryOperatorType.InEquality &&
//				    binaryOperatorExpression.Operator != BinaryOperatorType.GreaterThan &&
//				    binaryOperatorExpression.Operator != BinaryOperatorType.GreaterThanOrEqual &&
//				    binaryOperatorExpression.Operator != BinaryOperatorType.LessThan &&
//				    binaryOperatorExpression.Operator != BinaryOperatorType.LessThanOrEqual) {
//					return;
//				}
//
//				var match = pattern.Match(binaryOperatorExpression);
//				if (match.Success) {
//					AddIssue(binaryOperatorExpression, binaryOperatorExpression.OperatorToken, binaryOperatorExpression.Operator == BinaryOperatorType.Equality);
//					return;
//				}
//			}
//
//			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
//			{
//				base.VisitInvocationExpression(invocationExpression);
//				var rr = ctx.Resolve(invocationExpression) as InvocationResolveResult;
//				if (rr == null || rr.Member.Name != "Equals" || !rr.Member.ReturnType.IsKnownType(KnownTypeCode.Boolean))
//					return;
//
//				if (rr.Member.IsStatic) {
//					if (rr.Member.Parameters.Count != 2)
//						return;
//					if (CSharpUtil.AreConditionsEqual(invocationExpression.Arguments.FirstOrDefault(), invocationExpression.Arguments.Last())) {
//						if ((invocationExpression.Parent is UnaryOperatorExpression) && ((UnaryOperatorExpression)invocationExpression.Parent).Operator == UnaryOperatorType.Not) {
//							AddIssue(invocationExpression.Parent, invocationExpression.Parent, false);
//						} else {
//							AddIssue(invocationExpression, invocationExpression, true);
//						}
//					}
//				} else {
//					if (rr.Member.Parameters.Count != 1)
//						return;
//					var target = invocationExpression.Target as MemberReferenceExpression;
//					if (target == null)
//						return;
//					if (CSharpUtil.AreConditionsEqual(invocationExpression.Arguments.FirstOrDefault(), target.Target)) {
//						if ((invocationExpression.Parent is UnaryOperatorExpression) && ((UnaryOperatorExpression)invocationExpression.Parent).Operator == UnaryOperatorType.Not) {
//							AddIssue(invocationExpression.Parent, invocationExpression.Parent, false);
//						} else {
//							AddIssue(invocationExpression, invocationExpression, true);
//						}
//					}
//				}
//			}
		}
	}

	[ExportCodeFixProvider(EqualExpressionComparisonIssue.DiagnosticId, LanguageNames.CSharp)]
	public class EqualExpressionComparisonFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return EqualExpressionComparisonIssue.DiagnosticId;
		}

		public override async Task<IEnumerable<CodeAction>> GetFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, diagonstic.GetMessage(), document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}