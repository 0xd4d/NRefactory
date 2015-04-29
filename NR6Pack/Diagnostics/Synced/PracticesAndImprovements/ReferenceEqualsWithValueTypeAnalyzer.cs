﻿// 
// ReferenceEqualsCalledWithValueTypeAnalyzer.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
    [NotPortedYet]
    public class ReferenceEqualsWithValueTypeAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ReferenceEqualsWithValueTypeAnalyzerID, 
			GettextCatalog.GetString("Check for reference equality instead"),
			GettextCatalog.GetString("'Object.ReferenceEquals' is always false because it is called with value type"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ReferenceEqualsWithValueTypeAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			//context.RegisterSyntaxNodeAction(
			//	(nodeContext) => {
			//		Diagnostic diagnostic;
			//		if (TryGetDiagnostic (nodeContext, out diagnostic)) {
			//			nodeContext.ReportDiagnostic(diagnostic);
			//		}
			//	}, 
			//	new SyntaxKind[] { SyntaxKind.None }
			//);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			//var node = nodeContext.Node as ;
			//diagnostic = Diagnostic.Create (descriptor, node.GetLocation ());
			//return true;
			return false;
		}

//		class GatherVisitor : GatherVisitorBase<ReferenceEqualsWithValueTypeAnalyzer>
//		{
//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base (semanticModel, addDiagnostic, cancellationToken)
//			{
//			}

////			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
////			{
////				base.VisitInvocationExpression(invocationExpression);
////
////				// Quickly determine if this invocation is eligible to speed up the inspector
////				var nameToken = invocationExpression.Target.GetChildByRole(Roles.Identifier);
////				if (nameToken.Name != "ReferenceEquals")
////					return;
////
////				var resolveResult = ctx.Resolve(invocationExpression) as InvocationResolveResult;
////				if (resolveResult == null ||
////				    resolveResult.Member.DeclaringTypeDefinition == null ||
////				    resolveResult.Member.DeclaringTypeDefinition.KnownTypeCode != KnownTypeCode.Object ||
////				    resolveResult.Member.Name != "ReferenceEquals" ||
////				    invocationExpression.Arguments.All(arg => ctx.Resolve(arg).Type.IsReferenceType ?? true))
////					return;
////
////				var action1 = new CodeAction(ctx.TranslateString("Replace expression with 'false'"),
////					              script => script.Replace(invocationExpression, new PrimitiveExpression(false)), invocationExpression);
////
////				var action2 = new CodeAction(ctx.TranslateString("Use Equals()"),
////					              script => script.Replace(invocationExpression.Target,
////						              new PrimitiveType("object").Member("Equals")), invocationExpression);
////
////				AddDiagnosticAnalyzer(new CodeIssue(invocationExpression,
////					ctx.TranslateString("'Object.ReferenceEquals' is always false because it is called with value type"),
////					new [] { action1, action2 }));
////			}
//		}
	}
}