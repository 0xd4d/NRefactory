//
// ConvertToLambdaExpressionIssue.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ConvertToLambdaExpression")]
	public class ConvertToLambdaExpressionIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ConvertToLambdaExpressionIssue";
		const string Description            = "Convert to lambda with expression";
		const string MessageFormat          = "Can be converted to expression";
		const string Category               = IssueCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Convert to lambda expression");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}
 
		class GatherVisitor : GatherVisitorBase<ConvertToLambdaExpressionIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
//			{
//				base.VisitLambdaExpression(lambdaExpression);
//				BlockStatement block;
//				Expression expr;
//				if (!ConvertLambdaBodyStatementToExpressionAction.TryGetConvertableExpression(lambdaExpression.Body, out block, out expr))
//					return;
//				var node = block.Statements.FirstOrDefault() ?? block;
//				var expressionStatement = node as ExpressionStatement;
//				if (expressionStatement != null) {
//					if (expressionStatement.Expression is AssignmentExpression)
//						return;
//				}
//				var returnTypes = new List<IType>();
//				foreach (var type in TypeGuessing.GetValidTypes(ctx.Resolver, lambdaExpression)) {
//					if (type.Kind != TypeKind.Delegate)
//						continue;
//					var invoke = type.GetDelegateInvokeMethod();
//					if (!returnTypes.Contains(invoke.ReturnType))
//						returnTypes.Add(invoke.ReturnType);
//				}
//				if (returnTypes.Count > 1)
//					return;
//
//				AddIssue(new CodeIssue(
//					node,
//					ctx.TranslateString(""),
//					ConvertLambdaBodyStatementToExpressionAction.CreateAction(ctx, node, block, expr)
//				));
//			}
//
//			public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
//			{
//				base.VisitAnonymousMethodExpression(anonymousMethodExpression);
//				if (!anonymousMethodExpression.HasParameterList)
//					return;
//				BlockStatement block;
//				Expression expr;
//				if (!ConvertLambdaBodyStatementToExpressionAction.TryGetConvertableExpression(anonymousMethodExpression.Body, out block, out expr))
//					return;
//				var node = block.Statements.FirstOrDefault() ?? block;
//				var returnTypes = new List<IType>();
//				foreach (var type in TypeGuessing.GetValidTypes(ctx.Resolver, anonymousMethodExpression)) {
//					if (type.Kind != TypeKind.Delegate)
//						continue;
//					var invoke = type.GetDelegateInvokeMethod();
//					if (!returnTypes.Contains(invoke.ReturnType))
//						returnTypes.Add(invoke.ReturnType);
//				}
//				if (returnTypes.Count > 1)
//					return;
//
//				AddIssue(new CodeIssue(
//					node,
//					ctx.TranslateString(""),
//					ctx.TranslateString("Convert to lambda expression"),
//					script => {
//						var lambdaExpression = new LambdaExpression();
//						foreach (var parameter in anonymousMethodExpression.Parameters)
//							lambdaExpression.Parameters.Add(parameter.Clone());
//						lambdaExpression.Body = expr.Clone();
//						script.Replace(anonymousMethodExpression, lambdaExpression);
//					}
//				));
//			}
		}
	}

	[ExportCodeFixProvider(ConvertToLambdaExpressionIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ConvertToLambdaExpressionFixProvider : NRefactoryCodeFixProvider
	{
		public override IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return ConvertToLambdaExpressionIssue.DiagnosticId;
		}

		public override async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, "Convert to expression", document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}