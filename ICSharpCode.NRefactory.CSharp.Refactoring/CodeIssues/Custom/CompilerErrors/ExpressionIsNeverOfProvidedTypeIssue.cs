﻿// 
// ExpressionIsNeverOfProvidedTypeIssue.cs
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
	public class ExpressionIsNeverOfProvidedTypeIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ExpressionIsNeverOfProvidedTypeIssue";
		const string Description            = "CS0184:Given expression is never of the provided type";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.CompilerWarnings;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "CS0184:Given expression is never of the provided type");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ExpressionIsNeverOfProvidedTypeIssue>
		{
			//readonly CSharpConversions conversions;

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
				//conversions = CSharpConversions.Get(ctx.Compilation);
			}

//			public override void VisitIsExpression(IsExpression isExpression)
//			{
//				base.VisitIsExpression(isExpression);
//
////				var conversions = CSharpConversions.Get(ctx.Compilation);
//				var exprType = ctx.Resolve(isExpression.Expression).Type;
//				var providedType = ctx.ResolveType(isExpression.Type);
//
//				if (exprType.Kind == TypeKind.Unknown || providedType.Kind == TypeKind.Unknown)
//					return;
//				if (IsValidReferenceOrBoxingConversion(exprType, providedType))
//					return;
//				
//				var exprTP = exprType as ITypeParameter;
//				var providedTP = providedType as ITypeParameter;
//				if (exprTP != null) {
//					if (IsValidReferenceOrBoxingConversion(exprTP.EffectiveBaseClass, providedType)
//					    && exprTP.EffectiveInterfaceSet.All(i => IsValidReferenceOrBoxingConversion(i, providedType)))
//						return;
//				}
//				if (providedTP != null) {
//					if (IsValidReferenceOrBoxingConversion(exprType, providedTP.EffectiveBaseClass))
//						return;
//				}
//				
//				AddIssue(new CodeIssue(isExpression, ctx.TranslateString("Given expression is never of the provided type")));
//			}
//
//			bool IsValidReferenceOrBoxingConversion(IType fromType, IType toType)
//			{
//				Conversion c = conversions.ExplicitConversion(fromType, toType);
//				return c.IsValid && (c.IsIdentityConversion || c.IsReferenceConversion || c.IsBoxingConversion || c.IsUnboxingConversion);
//			}
		}
	}

	[ExportCodeFixProvider(ExpressionIsNeverOfProvidedTypeIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ExpressionIsNeverOfProvidedTypeFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ExpressionIsNeverOfProvidedTypeIssue.DiagnosticId;
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
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, diagonstic.GetMessage(), document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}