//
// ConvertIfStatementToConditionalTernaryExpressionIssue.cs
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
	[ExportDiagnosticAnalyzer("'if' statement can be re-written as '?:' expression", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "Convert 'if' to '?:'", AnalysisDisableKeyword = "ConvertIfStatementToConditionalTernaryExpression")]
	public class ConvertIfStatementToConditionalTernaryExpressionIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ConvertIfStatementToConditionalTernaryExpressionIssue";
		const string Description            = "Convert to '?:' expression";
		const string MessageFormat          = "Convert to '?:' expression";
		const string Category               = IssueCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

//		public static bool IsComplexExpression(AstNode expr)
//		{
//			return expr.StartLocation.Line != expr.EndLocation.Line ||
//			expr is ConditionalExpression ||
//			expr is BinaryOperatorExpression;
//		}
//
//		public static bool IsComplexCondition(Expression expr)
//		{
//			if (expr.StartLocation.Line != expr.EndLocation.Line)
//				return true;
//
//			if (expr is PrimitiveExpression || expr is IdentifierExpression || expr is MemberReferenceExpression || expr is InvocationExpression)
//				return false;
//
//			var pexpr = expr as ParenthesizedExpression;
//			if (pexpr != null)
//				return IsComplexCondition(pexpr.Expression);
//
//			var uOp = expr as UnaryOperatorExpression;
//			if (uOp != null)
//				return IsComplexCondition(uOp.Expression);
//
//			var bop = expr as BinaryOperatorExpression;
//			if (bop == null)
//				return true;
//			return !(bop.Operator == BinaryOperatorType.GreaterThan ||
//			bop.Operator == BinaryOperatorType.GreaterThanOrEqual ||
//			bop.Operator == BinaryOperatorType.Equality ||
//			bop.Operator == BinaryOperatorType.InEquality ||
//			bop.Operator == BinaryOperatorType.LessThan ||
//			bop.Operator == BinaryOperatorType.LessThanOrEqual);
//		}
//
		class GatherVisitor : GatherVisitorBase<ConvertIfStatementToConditionalTernaryExpressionIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
//			{
//				base.VisitIfElseStatement(ifElseStatement);
//				Match match;
//				if (!ConvertIfStatementToConditionalTernaryExpressionAction.GetMatch(ifElseStatement, out match))
//					return;
//				var target = match.Get<Expression>("target").Single();
//				var condition = match.Get<Expression>("condition").Single();
//				var trueExpr = match.Get<Expression>("expr1").Single();
//				var falseExpr = match.Get<Expression>("expr2").Single();
//
//				if (IsComplexCondition(condition) || IsComplexExpression(trueExpr) || IsComplexExpression(falseExpr))
//					return;
//				AddIssue(new CodeIssue(
//					ifElseStatement.IfToken,
//					ctx.TranslateString("")
//				){ IssueMarker = IssueMarker.DottedLine, ActionProvider = { typeof(ConvertIfStatementToConditionalTernaryExpressionAction) } });
//			}
		}
	}

	[ExportCodeFixProvider(ConvertIfStatementToConditionalTernaryExpressionIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ConvertIfStatementToConditionalTernaryExpressionFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return ConvertIfStatementToConditionalTernaryExpressionIssue.DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
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