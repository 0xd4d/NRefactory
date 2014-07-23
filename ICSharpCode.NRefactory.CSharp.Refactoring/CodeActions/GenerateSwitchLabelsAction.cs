﻿// 
// GenerateSwitchLabels.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Creates switch lables for enumerations")]
	[ExportCodeRefactoringProvider("Generate switch labels", LanguageNames.CSharp)]
	public class GenerateSwitchLabelsAction : ICodeRefactoringProvider
	{
		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
		{
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var switchStatement = root.FindNode(span) as SwitchStatementSyntax;
			if (switchStatement == null)
				return Enumerable.Empty<CodeAction>();

			var result = model.GetSymbolInfo(switchStatement.Expression);
			var resultType = result.Symbol.GetReturnType();
			if (resultType.TypeKind != TypeKind.Enum)
				return Enumerable.Empty<CodeAction>();

			if (switchStatement.Sections.Count == 0) {
				SyntaxList<SwitchSectionSyntax> sections = new SyntaxList<SwitchSectionSyntax>();
				foreach (var field in resultType.GetMembers().OfType<IFieldSymbol>()) {
					if (!field.IsConst)
						continue;
					sections = sections.Add(GetSectionFromSymbol(model, field, span).WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.BreakStatement())));
				}
				sections = sections.Add(SyntaxFactory.SwitchSection()
					.WithLabels(SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.SwitchLabel(SyntaxKind.DefaultSwitchLabel)))
					.WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(
						SyntaxFactory.ThrowStatement(
						SyntaxFactory.ObjectCreationExpression(
							SyntaxFactory.IdentifierName("ArgumentOutOfRangeException")).WithArgumentList(SyntaxFactory.ArgumentList())))));
				var newRoot = root.ReplaceNode(switchStatement, switchStatement.WithSections(sections).WithAdditionalAnnotations(Formatter.Annotation));
				return new[] { CodeActionFactory.Create(span, DiagnosticSeverity.Info, "Create switch labels", document.WithSyntaxRoot(newRoot)) };
			} else {
				List<IFieldSymbol> fields = new List<IFieldSymbol>();
				foreach (var field in resultType.GetMembers()) {
					var fieldSymbol = field as IFieldSymbol;
					if (fieldSymbol == null || !fieldSymbol.IsConst)
						continue;
					if (!IsHandled(model, switchStatement, fieldSymbol))
						fields.Add(fieldSymbol);
				}
				if (fields.Count == 0)
					return Enumerable.Empty<CodeAction>();
				var newSections = new SyntaxList<SwitchSectionSyntax>().AddRange(
					fields.Select(f => GetSectionFromSymbol(model, f, span).WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.BreakStatement()))));

				//default section - if it exists, remove it, add in our new sections, and replace it
				var defaultSection = switchStatement.Sections.FirstOrDefault(s => s.Labels.Any(l => l.Value == null));

				if (defaultSection == null)
					newSections = switchStatement.Sections.AddRange(newSections);
				else
					newSections = switchStatement.Sections.Remove(defaultSection).AddRange(newSections).Add(defaultSection);

				var newRoot = root.ReplaceNode(switchStatement, switchStatement.WithSections(newSections).WithAdditionalAnnotations(Formatter.Annotation));
				return new[] { CodeActionFactory.Create(span, DiagnosticSeverity.Info, "Create missing switch labels", document.WithSyntaxRoot(newRoot)) };
			}
		}

		private SwitchSectionSyntax GetSectionFromSymbol(SemanticModel model, IFieldSymbol field, TextSpan span)
		{
			return SyntaxFactory.SwitchSection().WithLabels(SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.SwitchLabel(SyntaxKind.CaseSwitchLabel,
				SyntaxFactory.IdentifierName(field.ToMinimalDisplayString(model, span.Start))))).WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.BreakStatement()));
		}

		private bool IsHandled(SemanticModel model, SwitchStatementSyntax switchStatement, IFieldSymbol field)
		{
			//if any label in any section has a value equal to the field name, return true
			return switchStatement.Sections.Any(s => s.Labels.Any(l => l.Value != null && ((MemberAccessExpressionSyntax)l.Value).Name.Identifier.ValueText.Equals(field.Name)));
		}
	}
}
