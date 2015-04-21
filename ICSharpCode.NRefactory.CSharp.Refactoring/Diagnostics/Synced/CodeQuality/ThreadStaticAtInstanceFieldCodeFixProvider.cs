//
// ThreadStaticAtInstanceFieldCodeFixProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ThreadStaticAtInstanceFieldCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (NRefactoryDiagnosticIDs.ThreadStaticAtInstanceFieldAnalyzerID);
			}
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var diagnostic = diagnostics.First ();
			var node = root.FindToken(context.Span.Start).Parent.AncestorsAndSelf ().OfType<AttributeSyntax> ().FirstOrDefault ();
			if (node == null)
				return;
			context.RegisterCodeFix(
				CodeActionFactory.Create(
					node.Span, 
					diagnostic.Severity, 
					GettextCatalog.GetString ("Remove attribute"),
					(arg) => {
						var list = node.Parent as AttributeListSyntax;
						if (list.Attributes.Count == 1) {
							var newRoot = root.RemoveNode (list, SyntaxRemoveOptions.KeepNoTrivia);
							return Task.FromResult (document.WithSyntaxRoot (newRoot));
						}
						var newRoot2 = root.RemoveNode (node, SyntaxRemoveOptions.KeepNoTrivia);
						return Task.FromResult (document.WithSyntaxRoot (newRoot2));
					}
				), 
				diagnostic
			);

			context.RegisterCodeFix(
				CodeActionFactory.Create(
					node.Span, 
					diagnostic.Severity,
					GettextCatalog.GetString ("Make the field static"),
					(arg) => {
						var field = node.Parent.Parent as FieldDeclarationSyntax;
						var newRoot = root.ReplaceNode (field, field.AddModifiers (SyntaxFactory.Token (SyntaxKind.StaticKeyword)).WithAdditionalAnnotations (Formatter.Annotation));
						return Task.FromResult (document.WithSyntaxRoot (newRoot));
					}
				), 
				diagnostic
			);
		}
	}
}