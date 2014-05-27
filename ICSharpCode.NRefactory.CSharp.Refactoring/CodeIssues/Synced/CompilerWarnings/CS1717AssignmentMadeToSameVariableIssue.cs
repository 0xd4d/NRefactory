﻿// 
// CS1717AssignmentMadeToSameVariableIssue.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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
	[ExportDiagnosticAnalyzer("CS1717:Assignment made to same variable", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "CSharpWarnings::CS1717", PragmaWarning = 1717)]
	public class CS1717AssignmentMadeToSameVariableIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "CS1717AssignmentMadeToSameVariableIssue";
		const string Description            = "CS1717:Assignment made to same variable";
		const string MessageFormat          = "CS1717:Assignment made to same variable";
		const string Category               = IssueCategories.CompilerWarnings;

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

		class GatherVisitor : GatherVisitorBase<CS1717AssignmentMadeToSameVariableIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitAssignmentExpression (AssignmentExpression assignmentExpression)
//			{
//				base.VisitAssignmentExpression (assignmentExpression);
//
//				if (assignmentExpression.Operator != AssignmentOperatorType.Assign)
//					return;
//				if (!(assignmentExpression.Left is IdentifierExpression) && 
//					!(assignmentExpression.Left is MemberReferenceExpression))
//					return;
//
//				var resolveResult = ctx.Resolve (assignmentExpression.Left);
//				var memberResolveResult = resolveResult as MemberResolveResult;
//				if (memberResolveResult != null) {
//					var memberResolveResult2 = ctx.Resolve (assignmentExpression.Right) as MemberResolveResult;
//					if (memberResolveResult2 == null || !AreEquivalent(memberResolveResult, memberResolveResult2))
//						return;
//				} else if (resolveResult is LocalResolveResult) {
//					if (!assignmentExpression.Left.Match (assignmentExpression.Right).Success)
//						return;
//				} else {
//					return;
//				}
//
//				AstNode node;
//				Action<Script> action;
//				if (assignmentExpression.Parent is ExpressionStatement) {
//					node = assignmentExpression.Parent;
//					action = script => script.Remove (assignmentExpression.Parent);
//				} else {
//					node = assignmentExpression;
//					action = script => script.Replace (assignmentExpression, assignmentExpression.Left.Clone ());
//				}
//				AddIssue (new CodeIssue(node, ctx.TranslateString (""),
//					new [] { new CodeAction (ctx.TranslateString (""), action, node) })
//					{ IssueMarker = IssueMarker.GrayOut }
//				);
//			}
//
//			static bool AreEquivalent(ResolveResult first, ResolveResult second)
//			{
//				var firstPath = AccessPath.FromResolveResult(first);
//				var secondPath = AccessPath.FromResolveResult(second);
//				return firstPath != null && firstPath.Equals(secondPath) && !firstPath.MemberPath.Any(m => !(m is IField));
//			}
		}
	}

	[ExportCodeFixProvider(CS1717AssignmentMadeToSameVariableIssue.DiagnosticId, LanguageNames.CSharp)]
	public class CS1717AssignmentMadeToSameVariableFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return CS1717AssignmentMadeToSameVariableIssue.DiagnosticId;
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
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, "Remove assignment", document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}