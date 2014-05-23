//
// RedundantComparisonWithNullIssue.cs
//
// Author:
//	   Ji Kun <jikun.nus@gmail.com>
//
// Copyright (c) 2013 Ji Kun
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
// THE SOFTWARE.using System;

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
	[ExportDiagnosticAnalyzer("Redundant comparison with 'null'", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "When 'is' keyword is used, which implicitly check null.", AnalysisDisableKeyword = "RedundantComparisonWithNull")]
	public class RedundantComparisonWithNullIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "RedundantComparisonWithNullIssue";
		const string Description            = "Redundant comparison with 'null'";
		const string MessageFormat          = "Remove expression";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantComparisonWithNullIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
//			{
//				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
//				Match m1 = pattern1.Match(binaryOperatorExpression);
//				if (m1.Success) {
//					AddIssue(new CodeIssue(binaryOperatorExpression,
//					         ctx.TranslateString(""),
//					         ctx.TranslateString(""), 
//					         script => {
//					         	var isExpr = m1.Get<AstType>("t").Single().Parent;
//					         	script.Replace(binaryOperatorExpression, isExpr);
//					         }
//					) { IssueMarker = IssueMarker.GrayOut });
//					return;
//				}
//			}
		}

//		private static readonly Pattern pattern1
//		= new Choice {
//			//  a is Record && a != null
//			new BinaryOperatorExpression(
//				PatternHelper.OptionalParentheses(
//					new IsExpression {
//						Expression = new AnyNode("a"),
//						Type = new AnyNode("t")
//					}),
//				BinaryOperatorType.ConditionalAnd,
//				PatternHelper.CommutativeOperatorWithOptionalParentheses(new Backreference("a"),
//					BinaryOperatorType.InEquality,
//					new NullReferenceExpression())
//			),
//			//  a != null && a is Record
//			new BinaryOperatorExpression (
//				PatternHelper.CommutativeOperatorWithOptionalParentheses(new AnyNode("a"),
//					BinaryOperatorType.InEquality,
//					new NullReferenceExpression()),
//				BinaryOperatorType.ConditionalAnd,
//				PatternHelper.OptionalParentheses(
//					new IsExpression {
//						Expression = new Backreference("a"),
//						Type = new AnyNode("t")
//					})
//			)
//		};

	}

	[ExportCodeFixProvider(RedundantComparisonWithNullIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantComparisonWithNullFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return RedundantComparisonWithNullIssue.DiagnosticId;
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