//
// CreateChangedEvent.cs
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
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Creates a changed event for an property.")]
	[ExportCodeRefactoringProvider("Create changed event for property", LanguageNames.CSharp)]
	public class CreateChangedEventAction : CodeRefactoringProvider
	{
		public override async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var property = root.FindNode(span) as PropertyDeclarationSyntax;

			if (property == null || !property.Identifier.Span.Contains(span))
				return Enumerable.Empty<CodeAction>();

			var field = RemoveBackingStoreAction.GetBackingField(model, property);
			if (field == null)
				return Enumerable.Empty<CodeAction>();
			var type = property.Parent as TypeDeclarationSyntax;
			if (type == null)
				return Enumerable.Empty<CodeAction>();

			var resolvedType = model.Compilation.GetTypeSymbol("System", "EventHandler", 0, cancellationToken);
			if (resolvedType == null)
				return Enumerable.Empty<CodeAction>();

			return new[] {
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info,
					"Create changed event",
					t2 => {
						var eventDeclaration = CreateChangedEventDeclaration (property);
						var methodDeclaration = CreateEventInvocatorAction.CreateEventInvocator (
							model,
							type, 
							eventDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
							eventDeclaration.Declaration.Variables.First().Identifier.ToString(),
							resolvedType.GetDelegateInvokeMethod (), 
							false
						);
						var invocation = SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
							SyntaxFactory.IdentifierName(methodDeclaration.Identifier.ToString()),
							SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(new [] { SyntaxFactory.Argument(SyntaxFactory.ParseExpression("System.EventArgs.Empty")) }))
						));

//				script.InsertWithCursor(
//					context.TranslateString("Create event invocator"),
//					Script.InsertPosition.After,
//					new AstNode[] { eventDeclaration, methodDeclaration }
//				).ContinueScript(delegate {
//					script.InsertBefore (property.Setter.Body.RBraceToken, stmt);
//					script.FormatText (stmt);
//				});
						var newRoot = root.InsertNodesAfter(property, new SyntaxNode[] { 
							methodDeclaration.WithAdditionalAnnotations(Formatter.Annotation), 
							eventDeclaration.WithAdditionalAnnotations(Formatter.Annotation)
						} ).InsertNodesAfter(
							property.AccessorList.Accessors.First(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)).Body.Statements.First(),
							new [] { invocation.WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation) }
						);

						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					})
			};
		}


		static EventFieldDeclarationSyntax CreateChangedEventDeclaration(PropertyDeclarationSyntax propertyDeclaration)
		{
			return SyntaxFactory.EventFieldDeclaration(
				SyntaxFactory.List<AttributeListSyntax>(),
				propertyDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) ?
				SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)) :
					SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
				SyntaxFactory.VariableDeclaration(
					SyntaxFactory.ParseTypeName("System.EventHandler").WithAdditionalAnnotations(Simplifier.Annotation),
					SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>( new [] { 
						SyntaxFactory.VariableDeclarator(propertyDeclaration.Identifier + "Changed")
					}
					)
				)
			);
		}
	}
}

