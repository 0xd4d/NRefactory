//
// RedundantArgumentDefaultValueCodeFixProvider.cs
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
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class RedundantArgumentDefaultValueCodeFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantArgumentDefaultValueAnalyzer.DiagnosticId;
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
			var node = root.FindNode(context.Span);
			//if (!node.IsKind(SyntaxKind.BaseList))
			//	continue;
			var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
			 
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove redundant arguments", document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}

	/* TODO: Merge:
[IssueDescription("Optional argument has default value and can be skipped",
	                  Description = "Finds calls to functions where optional parameters are used and the passed argument is the same as the default.",
	                  Category = IssueCategories.RedundanciesInCode,
	                  Severity = Severity.Hint)]
	public class OptionalParameterCouldBeSkippedAnalyzer : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseSemanticModel context)
		{
			return new GatherVisitor(context);
		}
		
		class GatherVisitor : GatherVisitorBase<OptionalParameterCouldBeSkippedAnalyzer>
		{
			static readonly object removeAllRedundantArgumentsKey = new object ();

			public GatherVisitor(BaseSemanticModel context) : base (context)
			{
			}

			public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
			{
				base.VisitObjectCreateExpression(objectCreateExpression);
				
				CheckMethodCall(objectCreateExpression, objectCreateExpression.Arguments,
				                (objectCreation, args) => new ObjectCreateExpression(objectCreation.Type.Clone(), args));
			}

			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
			{
				base.VisitInvocationExpression(invocationExpression);
				
				CheckMethodCall(invocationExpression, invocationExpression.Arguments,
				                (invocation, args) => new InvocationExpression(invocation.Target.Clone(), args));
			}

			void CheckMethodCall<T> (T node, IEnumerable<Expression> args, Func<T, IEnumerable<Expression>, T> generateReplacement) where T: AstNode
			{
				// The first two checks are unnecessary, but eliminates the majority of calls early,
				// improving performance.
				var arguments = args.ToArray();
				if (arguments.Length == 0)
					return;
				var lastArg = arguments[arguments.Length - 1];
				if (!(lastArg is PrimitiveExpression || lastArg is NamedArgumentExpression))
					return;

				var invocationResolveResult = ctx.Resolve(node) as CSharpInvocationResolveResult;
				if (invocationResolveResult == null)
					return;
				
				string actionMessage = ctx.TranslateString("Remove redundant arguments");
				
				var redundantArguments = GetRedundantArguments(arguments, invocationResolveResult);
				var action = new CodeAction(actionMessage, script => {
					var newArgumentList = arguments
						.Where(arg => !redundantArguments.Contains(arg))
						.Select(arg => arg.Clone());
					var newInvocation = generateReplacement(node, newArgumentList);
					script.Replace(node, newInvocation);
				}, node, removeAllRedundantArgumentsKey);
				var issueMessage = ctx.TranslateString("Argument is identical to the default value");
				var lastPositionalArgument = redundantArguments.FirstOrDefault(expression => !(expression is NamedArgumentExpression));

				foreach (var argument in redundantArguments) {
					var localArgument = argument;
					var actions = new List<CodeAction>();
					actions.Add(action);

					if (localArgument is NamedArgumentExpression || localArgument == lastPositionalArgument) {
						var title = ctx.TranslateString("Remove this argument");
						actions.Add(new CodeAction(title, script => {
							var newArgumentList = arguments
								.Where(arg => arg != localArgument)
								.Select(arg => arg.Clone());
							var newInvocation = generateReplacement(node, newArgumentList);
							script.Replace(node, newInvocation);
						}, node, null));
					} else {
						var title = ctx.TranslateString("Remove this and the following positional arguments");
						actions.Add(new CodeAction(title, script => {
							var newArgumentList = arguments
								.Where(arg => arg.StartLocation < localArgument.StartLocation && !(arg is NamedArgumentExpression))
								.Select(arg => arg.Clone());
							var newInvocation = generateReplacement(node, newArgumentList);
							script.Replace(node, newInvocation);
						}, node, null));
					}

					AddDiagnosticAnalyzer(new CodeIssue(localArgument, issueMessage, actions) { IssueMarker = IssueMarker.GrayOut });
				}
			}

			IList<Expression> GetRedundantArguments(Expression[] arguments, CSharpInvocationResolveResult invocationResolveResult)
			{
				var argumentToParameterMap = invocationResolveResult.GetArgumentToParameterMap();
				var resolvedParameters = invocationResolveResult.Member.Parameters;

				IList<Expression> redundantArguments = new List<Expression>();

				for (int i = arguments.Length - 1; i >= 0; i--) {
					var parameterIndex = argumentToParameterMap[i];
					if (parameterIndex == -1)
						// This particular parameter is an error, but keep trying the other ones
						continue;
					var parameter = resolvedParameters[parameterIndex];
					var argument = arguments[i];
					if (argument is PrimitiveExpression) {
						if (parameter.IsParams)
							// before positional params arguments all optional arguments are needed, otherwise some of the
							// param arguments will be shifted out of the params into the fixed parameters
							break;
						if (!parameter.IsOptional)
							// There can be no optional parameters preceding a required one
							break;
						var argumentResolveResult = ctx.Resolve(argument) as ConstantResolveResult;
						if (argumentResolveResult == null || parameter.ConstantValue != argumentResolveResult.ConstantValue)
							// Stop here since any arguments before this one has to be there
							// to enable the passing of this argument
							break;
						redundantArguments.Add(argument);
					} else if (argument is NamedArgumentExpression) {
						var expression = ((NamedArgumentExpression)argument).Expression as PrimitiveExpression;
						if (expression == null)
							continue;
						var expressionResolveResult = ctx.Resolve(expression) as ConstantResolveResult;
						if (expressionResolveResult == null || parameter.ConstantValue != expressionResolveResult.ConstantValue)
							// continue, since there can still be more arguments that are redundant
							continue;
						redundantArguments.Add(argument);
					} else {
						// This is a non-constant positional argument => no more redundancies are possible
						break;
					}
				}
				return redundantArguments;
			}
		}
	}
*/
}