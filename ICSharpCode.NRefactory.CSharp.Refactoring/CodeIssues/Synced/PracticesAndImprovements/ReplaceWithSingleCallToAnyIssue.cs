// 
// ReplaceWithSingleCallToAnyIssue.cs
//
// Author:
//       Mike Kr�ger <mkrueger@xamarin.com>
// 
// Copyright (c) 2013 Xamarin <http://xamarin.com>
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
	[ExportDiagnosticAnalyzer("Replace with single call to Any(...)", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "Replace with single call to Any(...)", AnalysisDisableKeyword = "ReplaceWithSingleCallToAny")]
	public class ReplaceWithSingleCallToAnyIssue : GatherVisitorCodeIssueProvider
	{
//		static readonly AstNode pattern =
//			new InvocationExpression (
//				new MemberReferenceExpression (
//					new NamedNode ("whereInvoke",
//					               new InvocationExpression (
//					               	new MemberReferenceExpression (new AnyNode ("target"), "Where"),
//					               	new AnyNode ())),
//					Pattern.AnyString));
		
		internal const string DiagnosticId  = "ReplaceWithSingleCallToAnyIssue";
		const string Description            = "Redundant Where() call with predicate followed by Any()";
		const string MessageFormat          = "Replace with single call to 'Any'";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor<ReplaceWithSingleCallToAnyIssue>(semanticModel, addDiagnostic, cancellationToken, "Any");
		}

		internal class GatherVisitor<T> : GatherVisitorBase<T> where T : GatherVisitorCodeIssueProvider
		{
			readonly string member;

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken, string member)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
				this.member = member;
			}

//			public override void VisitInvocationExpression (InvocationExpression anyInvoke)
//			{
//				base.VisitInvocationExpression (anyInvoke);
//				
//				var match = pattern.Match (anyInvoke);
//				if (!match.Success)
//					return;
//				
//				var anyResolve = ctx.Resolve (anyInvoke) as InvocationResolveResult;
//				if (anyResolve == null || !HasPredicateVersion(anyResolve.Member))
//					return;
//				var whereInvoke = match.Get<InvocationExpression> ("whereInvoke").Single ();
//				var whereResolve = ctx.Resolve (whereInvoke) as InvocationResolveResult;
//				if (whereResolve == null || whereResolve.Member.Name != "Where" || !IsQueryExtensionClass(whereResolve.Member.DeclaringTypeDefinition))
//					return;
//				if (whereResolve.Member.Parameters.Count != 2)
//					return;
//				var predResolve = whereResolve.Member.Parameters [1];
//				if (predResolve.Type.TypeParameterCount != 2)
//					return;
//				
//				AddIssue(new CodeIssue(
			//					anyInvoke, string.Format(ctx.TranslateString("Redundant Where() call with predicate followed by {0}()"), anyResolve.Member.Name),
//					new CodeAction (
			//						string.Format(ctx.TranslateString("Replace with single call to '{0}'"), anyResolve.Member.Name),
//						script => {
//							var arg = whereInvoke.Arguments.Single ().Clone ();
//							var target = match.Get<Expression> ("target").Single ().Clone ();
//							script.Replace (anyInvoke, new InvocationExpression (new MemberReferenceExpression (target, anyResolve.Member.Name), arg));
//						},
//						anyInvoke
//					)
//				));
//			}
//			
//			bool IsQueryExtensionClass(ITypeDefinition typeDef)
//			{
//				if (typeDef == null || typeDef.Namespace != "System.Linq")
//					return false;
//				switch (typeDef.Name) {
//					case "Enumerable":
//					case "ParallelEnumerable":
//					case "Queryable":
//						return true;
//					default:
//						return false;
//				}
//			}
//			
//			bool HasPredicateVersion(IParameterizedMember member)
//			{
//				if (!IsQueryExtensionClass(member.DeclaringTypeDefinition))
//					return false;
//			    return member.Name == this.member;
//			}
		}
	}

	[ExportCodeFixProvider(ReplaceWithSingleCallToAnyIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ReplaceWithSingleCallToAnyFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return ReplaceWithSingleCallToAnyIssue.DiagnosticId;
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