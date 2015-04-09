//
// ReplaceWithLastOrDefaultAnalyzer.cs
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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ReplaceWithLastOrDefaultAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ReplaceWithLastOrDefaultAnalyzerID, 
			GettextCatalog.GetString("Replace with call to LastOrDefault<T>()"),
			GettextCatalog.GetString("Expression can be simlified to 'LastOrDefault<T>()'"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ReplaceWithLastOrDefaultAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.ConditionalExpression
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			var node = nodeContext.Node as ConditionalExpressionSyntax;

			//pattern is Any(param) ? Last(param) : null/default
			var anyInvocation = node.Condition as InvocationExpressionSyntax;
			var lastInvocation = node.WhenTrue as InvocationExpressionSyntax;
			var nullDefaultWhenFalse = node.WhenFalse;

			if (anyInvocation == null || lastInvocation == null || nullDefaultWhenFalse == null)
				return false;
			var anyExpression = anyInvocation.Expression as MemberAccessExpressionSyntax;
			if (anyExpression == null || anyExpression.Name.Identifier.ValueText != "Any")
				return false;
			var anyParam = anyInvocation.ArgumentList;

			var lastExpression = lastInvocation.Expression as MemberAccessExpressionSyntax;
			if (lastExpression == null || lastExpression.Name.Identifier.ValueText != "Last" || !lastInvocation.ArgumentList.IsEquivalentTo(anyParam))
				return false;

			if (!nullDefaultWhenFalse.IsKind(SyntaxKind.NullLiteralExpression) && !nullDefaultWhenFalse.IsKind(SyntaxKind.DefaultExpression))
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				node.GetLocation ()
			);
			return true;
		}
	}
}