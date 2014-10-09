// 
// StringIsNullOrEmptyInspector.cs
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
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	/// <summary>
	/// Checks for str == null &amp;&amp; str == " "
	/// Converts to: string.IsNullOrEmpty (str)
	/// </summary>
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ReplaceWithStringIsNullOrEmpty")]
	public class ReplaceWithStringIsNullOrEmptyIssue : GatherVisitorCodeIssueProvider
	{
//		static readonly Pattern pattern = new Choice {
//			// str == null || str == ""
//			// str == null || str.Length == 0
//			new BinaryOperatorExpression (
//				PatternHelper.CommutativeOperatorWithOptionalParentheses (new AnyNode ("str"), BinaryOperatorType.Equality, new NullReferenceExpression ()),
//				BinaryOperatorType.ConditionalOr,
//				new Choice {
//					PatternHelper.CommutativeOperatorWithOptionalParentheses (new Backreference ("str"), BinaryOperatorType.Equality, new PrimitiveExpression ("")),
//					PatternHelper.CommutativeOperatorWithOptionalParentheses (new Backreference ("str"), BinaryOperatorType.Equality,
//				                                       new PrimitiveType("string").Member("Empty")),
//					PatternHelper.CommutativeOperatorWithOptionalParentheses (
//						new MemberReferenceExpression (new Backreference ("str"), "Length"),
//						BinaryOperatorType.Equality,
//						new PrimitiveExpression (0)
//					)
//				}
//			),
//			// str == "" || str == null
//			new BinaryOperatorExpression (
//				new Choice {
//					PatternHelper.CommutativeOperatorWithOptionalParentheses (new AnyNode ("str"), BinaryOperatorType.Equality, new PrimitiveExpression ("")),
//					PatternHelper.CommutativeOperatorWithOptionalParentheses (new AnyNode ("str"), BinaryOperatorType.Equality,
//				                                       new PrimitiveType("string").Member("Empty"))
//				},
//				BinaryOperatorType.ConditionalOr,
//				PatternHelper.CommutativeOperator(new Backreference ("str"), BinaryOperatorType.Equality, new NullReferenceExpression ())
//			)
//		};
//		static readonly Pattern negPattern = new Choice {
//			// str != null && str != ""
//			// str != null && str.Length != 0
//			// str != null && str.Length > 0
//			new BinaryOperatorExpression (
//				PatternHelper.CommutativeOperatorWithOptionalParentheses(new AnyNode ("str"), BinaryOperatorType.InEquality, new NullReferenceExpression ()),
//				BinaryOperatorType.ConditionalAnd,
//				new Choice {
//					PatternHelper.CommutativeOperatorWithOptionalParentheses (new Backreference ("str"), BinaryOperatorType.InEquality, new PrimitiveExpression ("")),
//					PatternHelper.CommutativeOperatorWithOptionalParentheses (new Backreference ("str"), BinaryOperatorType.InEquality,
//				                                   	   new PrimitiveType("string").Member("Empty")),
//					PatternHelper.CommutativeOperatorWithOptionalParentheses (
//						new MemberReferenceExpression (new Backreference ("str"), "Length"),
//						BinaryOperatorType.InEquality,
//						new PrimitiveExpression (0)
//					),
//					new BinaryOperatorExpression (
//						new MemberReferenceExpression (new Backreference ("str"), "Length"),
//						BinaryOperatorType.GreaterThan,
//						new PrimitiveExpression (0)
//					)
//				}
//			),
//			// str != "" && str != null
//			new BinaryOperatorExpression (
//				new Choice {
//					PatternHelper.CommutativeOperatorWithOptionalParentheses (new AnyNode ("str"), BinaryOperatorType.InEquality, new PrimitiveExpression ("")),
//					PatternHelper.CommutativeOperatorWithOptionalParentheses (new AnyNode ("str"), BinaryOperatorType.Equality,
//				                                   	   new PrimitiveType("string").Member("Empty"))
//				},
//				BinaryOperatorType.ConditionalAnd,
//				PatternHelper.CommutativeOperatorWithOptionalParentheses(new Backreference ("str"), BinaryOperatorType.InEquality, new NullReferenceExpression ())
//			)
//		};

		internal const string DiagnosticId  = "ReplaceWithStringIsNullOrEmptyIssue";
		const string Description            = "Uses shorter string.IsNullOrEmpty call instead of a longer condition.";
		const string MessageFormat          = "Expression can be replaced with '{0}'";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Use 'String.IsNullOrEmpty'");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ReplaceWithStringIsNullOrEmptyIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
//			{
//				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
//				Match m = pattern.Match(binaryOperatorExpression);
//				bool isNegated = false;
//				if (!m.Success) {
//					m = negPattern.Match(binaryOperatorExpression);
//					isNegated = true;
//				}
//				if (m.Success) {
//					var str = m.Get<Expression>("str").Single();
//					var def = ctx.Resolve(str).Type.GetDefinition();
//					if (def == null || def.KnownTypeCode != ICSharpCode.NRefactory.TypeSystem.KnownTypeCode.String)
//						return;
//					AddIssue(new CodeIssue(
//						binaryOperatorExpression,
			//						isNegated ? ctx.TranslateString("Expression can be replaced with !string.IsNullOrEmpty") : ctx.TranslateString(""),
//						new CodeAction (
			//							isNegated ? ctx.TranslateString("Use !string.IsNullOrEmpty") : ctx.TranslateString("Use string.IsNullOrEmpty"),
//							script => {
//								Expression expr = new PrimitiveType("string").Invoke("IsNullOrEmpty", str.Clone());
//								if (isNegated)
//									expr = new UnaryOperatorExpression(UnaryOperatorType.Not, expr);
//								script.Replace(binaryOperatorExpression, expr);
//							},
//							binaryOperatorExpression
//						)
//					));
//					return;
//				}
//			}
//	
		}
	}

	[ExportCodeFixProvider(ReplaceWithStringIsNullOrEmptyIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ReplaceWithStringIsNullOrEmptyFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ReplaceWithStringIsNullOrEmptyIssue.DiagnosticId;
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
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, "Use '{0}'", document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}