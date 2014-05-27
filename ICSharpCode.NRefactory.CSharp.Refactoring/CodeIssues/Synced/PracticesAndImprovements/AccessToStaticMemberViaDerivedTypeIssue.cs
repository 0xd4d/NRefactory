//
// CallToStaticMemberViaDerivedTypeIssue.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("Call to static member via a derived class", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "AccessToStaticMemberViaDerivedType")]
	public class AccessToStaticMemberViaDerivedTypeIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "AccessToStaticMemberViaDerivedTypeIssue";
		const string Description            = "Suggests using the class declaring a static function when calling it.";
		const string MessageFormat          = "Static method invoked via derived type";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<AccessToStaticMemberViaDerivedTypeIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
			{
				base.VisitMemberAccessExpression(node);
				if (node.Expression.IsKind(SyntaxKind.ThisExpression) || node.Expression.IsKind(SyntaxKind.BaseExpression))
					// Call within current class scope using 'this' or 'base'
					return;
				var memberResolveResult = semanticModel.GetSymbolInfo(node);
				if (memberResolveResult.Symbol == null)
					return;
				if (!memberResolveResult.Symbol.IsStatic)
					return;

				HandleMember(node, node.Expression, memberResolveResult, semanticModel.GetTypeInfo(node.Expression));
			}

			void HandleMember(SyntaxNode issueAnchor, SyntaxNode targetExpression, SymbolInfo targetResolveResult, TypeInfo typeInfo)
			{
				var rr = targetResolveResult;
				if (rr.Symbol == null || typeInfo.Type == null)
					return;
				if (!rr.Symbol.IsStatic)
					return;

				if (rr.Symbol.ContainingType.Equals(typeInfo.Type))
					return;

				// check whether member.DeclaringType contains the original type
				// (curiously recurring template pattern)
				if (CheckCuriouslyRecurringTemplatePattern(rr.Symbol.ContainingType, typeInfo.Type))
					return;

				AddIssue (Diagnostic.Create(Rule, Location.Create(semanticModel.SyntaxTree, targetExpression.Span)));
			}

			static bool CheckCuriouslyRecurringTemplatePattern(ITypeSymbol containingType, ITypeSymbol type)
			{
				if (containingType.Equals(type))
					return true;
				var nt = containingType as INamedTypeSymbol;
				if (nt == null)
					return false;
				foreach (var typeArg in nt.TypeArguments) {
					if (CheckCuriouslyRecurringTemplatePattern(typeArg, type))
						return true;
				}
				return false;
			}
		}
	}

	[ExportCodeFixProvider(AccessToStaticMemberViaDerivedTypeIssue.DiagnosticId, LanguageNames.CSharp)]
	public class AccessToStaticMemberViaDerivedTypeFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return AccessToStaticMemberViaDerivedTypeIssue.DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan);
				if (node == null)
					continue;
				var typeInfo = semanticModel.GetSymbolInfo(node.Parent);
				var newType = typeInfo.Symbol.ContainingType.ToMinimalDisplayString(semanticModel, node.SpanStart);
				var newRoot = root.ReplaceNode(node, SyntaxFactory.ParseTypeName(newType).WithLeadingTrivia(node.GetLeadingTrivia()));
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, string.Format("Use base qualifier '{0}'", newType), document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}