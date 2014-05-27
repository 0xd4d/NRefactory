//
// ThreadStaticAtInstanceFieldIssue.cs
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
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("[ThreadStatic] doesn't work with instance fields", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "[ThreadStatic] doesn't work with instance fields", AnalysisDisableKeyword = "ThreadStaticAtInstanceField")]
	public class ThreadStaticAtInstanceFieldIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ThreadStaticAtInstanceFieldIssue";
		const string Description            = "";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.CodeQualityIssues;

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

		class GatherVisitor : GatherVisitorBase<ThreadStaticAtInstanceFieldIssue>
		{
			//IType threadStaticAttribute;

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
				//threadStaticAttribute = ctx.Compilation.FindType(typeof(ThreadStaticAttribute));
			}

//			public override void VisitBlockStatement(BlockStatement blockStatement)
//			{
//			}
//			
//			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
//			{
//                if (fieldDeclaration.HasModifier(Modifiers.Static))
//                    return;
//
//                foreach (var attributeSection in fieldDeclaration.Attributes) {
//					int attributeCount = attributeSection.Attributes.Count;
//					foreach (var attribute in attributeSection.Attributes) {
//						var resolvedAttribute = ctx.Resolve(attribute.Type) as TypeResolveResult;
//						if (resolvedAttribute == null)
//							continue;
//						if (threadStaticAttribute.Equals(resolvedAttribute.Type)) {
//							string title = ctx.TranslateString("ThreadStatic does nothing on instance fields");
//							if (attributeCount == 1)
//								AddIssue(new CodeIssue(attributeSection, title, GetActions(attribute, attributeSection, fieldDeclaration)));
//							else
//								AddIssue(new CodeIssue(attribute, title, GetActions(attribute, attributeSection, fieldDeclaration)));
//						}
//					}
//				}
//			}
//
//			IEnumerable<CodeAction> GetActions(Attribute attribute, AttributeSection attributeSection, FieldDeclaration fieldDeclaration)
//			{
//				string removeAttributeMessage = ctx.TranslateString("Remove attribute");
//				yield return new CodeAction(removeAttributeMessage, script => {
//					if (attributeSection.Attributes.Count > 1) {
//						var newSection = new AttributeSection();
//						newSection.AttributeTarget = attributeSection.AttributeTarget;
//						foreach (var attr in attributeSection.Attributes) {
//							if (attr != attribute)
//								newSection.Attributes.Add((Attribute)attr.Clone());
//						}
//						script.Replace(attributeSection, newSection);
//					} else {
//						script.Remove(attributeSection);
//					}
//				}, attribute);
//
//				var makeStaticMessage = ctx.TranslateString("Make the field static");
//				yield return new CodeAction(makeStaticMessage, script => {
//					var newDeclaration = (FieldDeclaration)fieldDeclaration.Clone();
//					newDeclaration.Modifiers |= Modifiers.Static;
//					script.Replace(fieldDeclaration, newDeclaration);
//				}, fieldDeclaration.NameToken);
//			}
		}
	}

	[ExportCodeFixProvider(ThreadStaticAtInstanceFieldIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ThreadStaticAtInstanceFieldFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return ThreadStaticAtInstanceFieldIssue.DiagnosticId;
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