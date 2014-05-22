﻿// 
// FlipOperatorArguments.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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

using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Swaps left and right arguments.")]
	[ExportCodeRefactoringProvider("Swap left and right arguments", LanguageNames.CSharp)]
	public class FlipOperatorArgumentsAction : ICodeRefactoringProvider
	{
		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
		{
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var binop = root.FindToken(span.Start).Parent as BinaryExpressionSyntax;

			if (binop == null || !binop.OperatorToken.Span.Contains(span))
				return Enumerable.Empty<CodeAction>();

			return new[] {
				CodeActionFactory.Create(
					binop.OperatorToken.Span,
					DiagnosticSeverity.Info,
					string.Format("Flip '{0}' operator arguments", binop.OperatorToken),
					t2 => {
						var newBinop = SyntaxFactory.BinaryExpression(binop.CSharpKind(), binop.Right, binop.Left)
							.WithAdditionalAnnotations(Formatter.Annotation);
						var newRoot = root.ReplaceNode(binop, newBinop);
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			};
		}
	}
}
