//
// AdditionalOfTypeIssues.cs
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
	public class AdditionalOfTypeIssues : GatherVisitorCodeIssueProvider
	{
//		static readonly AstNode whereSimpleCase =
//			new InvocationExpression(
//				new MemberReferenceExpression(new AnyNode("target"), "Where"),
//				new NamedNode("lambda", 
//					new LambdaExpression {
//						Parameters = { PatternHelper.NamedParameter("param1", PatternHelper.AnyType("paramType", true), Pattern.AnyString) },
//						Body = PatternHelper.OptionalParentheses(
//								new IsExpression(PatternHelper.OptionalParentheses(new NamedNode("expr1", new IdentifierExpression(Pattern.AnyString))), new AnyNode("type"))
//						)
//					}
//				)
//			);

		internal const string DiagnosticId  = "AdditionalOfTypeIssues";
		const string Description            = "Replace with call to OfType<T> (extended cases)";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Replace with OfType<T> (extended)");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<AdditionalOfTypeIssues>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitInvocationExpression (InvocationExpression anyInvoke)
//			{
//				var match = ReplaceWithOfTypeIssue.selectNotNullPattern.Match (anyInvoke);
//				if (match.Success)
//					return;
//
//				match = ReplaceWithOfTypeIssue.wherePatternCase1.Match (anyInvoke);
//				if (match.Success)
//					return;
//
//				match = ReplaceWithOfTypeIssue.wherePatternCase2.Match (anyInvoke); 
//				if (match.Success)
//					return;
//
//				// Warning: The simple case is not 100% equal in semantic, but it's one common code smell
//				match = whereSimpleCase.Match (anyInvoke); 
//				if (!match.Success)
//					return;
//				var lambda = match.Get<LambdaExpression>("lambda").Single();
//				var expr = match.Get<IdentifierExpression>("expr1").Single();
//				if (lambda.Parameters.Count != 1)
//					return;
//				if (expr.Identifier != lambda.Parameters.Single().Name)
//					return;
//				AddIssue (new CodeIssue(
//					anyInvoke,
//					ctx.TranslateString("Replace with OfType<T>"),
//					ctx.TranslateString("Replace with call to OfType<T>"),
//					script => {
//						var target = match.Get<Expression>("target").Single().Clone ();
//						var type = match.Get<AstType>("type").Single().Clone();
//						script.Replace(anyInvoke, new InvocationExpression(new MemberReferenceExpression(target, "OfType", type)));
//					}
//				));
//			}
		}
	}

	[ExportCodeFixProvider(AdditionalOfTypeIssues.DiagnosticId, LanguageNames.CSharp)]
	public class AdditionalOfTypeFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return AdditionalOfTypeIssues.DiagnosticId;
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