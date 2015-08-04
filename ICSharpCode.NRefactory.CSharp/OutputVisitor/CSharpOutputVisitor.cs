// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Outputs the AST.
	/// </summary>
	public class CSharpOutputVisitor : IAstVisitor
	{
		readonly TokenWriter writer;
		readonly CSharpFormattingOptions policy;
		readonly Stack<AstNode> containerStack = new Stack<AstNode> ();
		
		public CSharpOutputVisitor (TextWriter textWriter, CSharpFormattingOptions formattingPolicy)
		{
			if (textWriter == null) {
				throw new ArgumentNullException ("textWriter");
			}
			if (formattingPolicy == null) {
				throw new ArgumentNullException ("formattingPolicy");
			}
			this.writer = TokenWriter.Create(textWriter);
			this.policy = formattingPolicy;
		}
		
		public CSharpOutputVisitor (TokenWriter writer, CSharpFormattingOptions formattingPolicy)
		{
			if (writer == null) {
				throw new ArgumentNullException ("writer");
			}
			if (formattingPolicy == null) {
				throw new ArgumentNullException ("formattingPolicy");
			}
			this.writer = new InsertSpecialsDecorator(new InsertRequiredSpacesDecorator(writer));
			this.policy = formattingPolicy;
		}
		
		#region StartNode/EndNode
		void StartNode(AstNode node)
		{
			// Ensure that nodes are visited in the proper nested order.
			// Jumps to different subtrees are allowed only for the child of a placeholder node.
			Debug.Assert(containerStack.Count == 0 || node.Parent == containerStack.Peek() || containerStack.Peek().NodeType == NodeType.Pattern);
			containerStack.Push(node);
			writer.StartNode(node);
		}
		
		void EndNode(AstNode node)
		{
			Debug.Assert(node == containerStack.Peek());
			containerStack.Pop();
			writer.EndNode(node);
		}
		#endregion
		
		#region debug statements
		int preventDebugStart = 0;
		void DebugStart(AstNode node, TextLocation? start = null)
		{
			if (++preventDebugStart == 1)
				writer.DebugStart(node, start);
		}

		void DebugStart(AstNode node, TokenRole role)
		{
			WriteKeyword(role, node);
		}

		void DebugHidden(AstNode hiddenNode)
		{
			writer.DebugHidden(hiddenNode);
		}

		void DebugExpression(AstNode node)
		{
			writer.DebugExpression(node);
		}

		void SemicolonDebugEnd(AstNode node)
		{
			Semicolon(node);
		}

		void DebugEnd(AstNode node, bool addSelf = true)
		{
			DebugEnd(node, null, addSelf);
		}

		void DebugEnd(AstNode node, TextLocation? end, bool addSelf = true)
		{
			if (addSelf)
				writer.DebugExpression(node);
			if (--preventDebugStart == 0)
				writer.DebugEnd(node, end);
		}
		#endregion
		
		#region Comma
		/// <summary>
		/// Writes a comma.
		/// </summary>
		/// <param name="nextNode">The next node after the comma.</param>
		/// <param name="noSpaceAfterComma">When set prevents printing a space after comma.</param>
		void Comma(AstNode nextNode, bool noSpaceAfterComma = false)
		{
			Space(policy.SpaceBeforeBracketComma);
			// TODO: Comma policy has changed.
			writer.WriteTokenOperator(Roles.Comma, ",");
			Space(!noSpaceAfterComma && policy.SpaceAfterBracketComma);
			// TODO: Comma policy has changed.
		}
		
		/// <summary>
		/// Writes an optional comma, e.g. at the end of an enum declaration or in an array initializer
		/// </summary>
		void OptionalComma(AstNode pos)
		{
			// Look if there's a comma after the current node, and insert it if it exists.
			while (pos != null && pos.NodeType == NodeType.Whitespace) {
				pos = pos.NextSibling;
			}
			if (pos != null && pos.Role == Roles.Comma) {
				Comma(null, noSpaceAfterComma: true);
			}
		}
		
		/// <summary>
		/// Writes an optional semicolon, e.g. at the end of a type or namespace declaration.
		/// </summary>
		void OptionalSemicolon(AstNode pos)
		{
			// Look if there's a semicolon after the current node, and insert it if it exists.
			while (pos != null && pos.NodeType == NodeType.Whitespace) {
				pos = pos.PrevSibling;
			}
			if (pos != null && pos.Role == Roles.Semicolon) {
				Semicolon();
			}
		}
		
		void WriteCommaSeparatedList(IEnumerable<AstNode> list)
		{
			bool isFirst = true;
			foreach (AstNode node in list) {
				if (isFirst) {
					isFirst = false;
				} else {
					Comma(node);
				}
				node.AcceptVisitor(this);
			}
		}
		
		void WriteCommaSeparatedListInParenthesis(IEnumerable<AstNode> list, bool spaceWithin)
		{
			LPar();
			if (list.Any()) {
				Space(spaceWithin);
				WriteCommaSeparatedList(list);
				Space(spaceWithin);
			}
			RPar();
		}
		
		#if DOTNET35
		void WriteCommaSeparatedList(IEnumerable<VariableInitializer> list)
		{
			WriteCommaSeparatedList(list.SafeCast<VariableInitializer, AstNode>());
		}
		
		void WriteCommaSeparatedList(IEnumerable<AstType> list)
		{
			WriteCommaSeparatedList(list.SafeCast<AstType, AstNode>());
		}
		
		void WriteCommaSeparatedListInParenthesis(IEnumerable<Expression> list, bool spaceWithin)
		{
			WriteCommaSeparatedListInParenthesis(list.SafeCast<Expression, AstNode>(), spaceWithin);
		}
		
		void WriteCommaSeparatedListInParenthesis(IEnumerable<ParameterDeclaration> list, bool spaceWithin)
		{
			WriteCommaSeparatedListInParenthesis(list.SafeCast<ParameterDeclaration, AstNode>(), spaceWithin);
		}

		#endif

		void WriteCommaSeparatedListInBrackets(IEnumerable<ParameterDeclaration> list, bool spaceWithin)
		{
			WriteToken(Roles.LBracket);
			if (list.Any()) {
				Space(spaceWithin);
				WriteCommaSeparatedList(list);
				Space(spaceWithin);
			}
			WriteToken(Roles.RBracket);
		}

		void WriteCommaSeparatedListInBrackets(IEnumerable<Expression> list)
		{
			WriteToken(Roles.LBracket);
			if (list.Any()) {
				Space(policy.SpacesWithinBrackets);
				WriteCommaSeparatedList(list);
				Space(policy.SpacesWithinBrackets);
			}
			WriteToken(Roles.RBracket);
		}
		#endregion
		
		#region Write tokens
		bool isAtStartOfLine = true;
		
		/// <summary>
		/// Writes a keyword, and all specials up to
		/// </summary>
		void WriteKeyword(TokenRole tokenRole, AstNode node = null)
		{
			WriteKeywordIdentifier(tokenRole.Token, tokenRole, node, false);
		}
		
		void WriteKeyword(string token, Role tokenRole = null, AstNode node = null)
		{
			WriteKeywordIdentifier(token, tokenRole, node, false);
		}

		void WriteKeywordIdentifier(TokenRole tokenRole)
		{
			WriteKeywordIdentifier(tokenRole.Token, tokenRole, null, true);
		}

		void WriteKeywordIdentifier(string token, Role tokenRole, AstNode node = null, bool isId = true)
		{
			if (node != null)
				DebugStart(node);
			if (isId)
				writer.WriteIdentifier(Identifier.Create(token), TextTokenType.Keyword);
			else
				writer.WriteKeyword(tokenRole, token);
			isAtStartOfLine = false;
		}
		
		void WriteIdentifier(Identifier identifier)
		{
			WriteIdentifier(identifier, identifier.AnnotationVT<TextTokenType>() ?? TextTokenType.Text);
		}

		void WriteIdentifier(Identifier identifier, TextTokenType tokenType)
		{
			writer.WriteIdentifier(identifier, tokenType);
			isAtStartOfLine = false;
		}
		
		void WriteIdentifier(string identifier, TextTokenType tokenType)
		{
			AstType.Create(identifier, tokenType).AcceptVisitor(this);
			isAtStartOfLine = false;
		}
		
		void WriteToken(TokenRole tokenRole)
		{
			WriteToken(tokenRole.Token, tokenRole);
		}
		
		void WriteToken(string token, Role tokenRole)
		{
			writer.WriteToken(tokenRole, token, token.GetTextTokenTypeFromLangToken());
			isAtStartOfLine = false;
		}
		
		void LPar()
		{
			WriteToken(Roles.LPar);
		}
		
		void RPar()
		{
			WriteToken(Roles.RPar);
		}
		
		/// <summary>
		/// Marks the end of a statement
		/// </summary>
		/// <param name="node">Statement node or null</param>
		void Semicolon(AstNode node = null)
		{
			Role role = containerStack.Peek().Role;
			// get the role of the current node
			if (!(role == ForStatement.InitializerRole || role == ForStatement.IteratorRole || role == UsingStatement.ResourceAcquisitionRole)) {
				WriteToken(Roles.Semicolon);
				if (node != null)
					DebugEnd(node);
				NewLine();
			}
			else if (node != null)
				DebugEnd(node);
		}
		
		/// <summary>
		/// Writes a space depending on policy.
		/// </summary>
		void Space(bool addSpace = true)
		{
			if (addSpace) {
				writer.Space();
			}
		}
		
		void NewLine()
		{
			writer.NewLine();
			isAtStartOfLine = true;
		}
		
		void OpenBrace(BraceStyle style)
		{
			TextLocation? start, end;
			OpenBrace(style, out start, out end);
		}

		void CloseBrace(BraceStyle style)
		{
			TextLocation? start, end;
			CloseBrace(style, out start, out end);
		}
		
		void OpenBrace(BraceStyle style, out TextLocation? start, out TextLocation? end)
		{
			switch (style) {
				case BraceStyle.DoNotChange:
				case BraceStyle.EndOfLine:
				case BraceStyle.BannerStyle:
					if (!isAtStartOfLine)
						writer.Space();
					start = writer.GetLocation();
					writer.WriteToken(Roles.LBrace, "{", TextTokenType.Brace);
					end = writer.GetLocation();
					break;
				case BraceStyle.EndOfLineWithoutSpace:
					start = writer.GetLocation();
					writer.WriteToken(Roles.LBrace, "{", TextTokenType.Brace);
					end = writer.GetLocation();
					break;
				case BraceStyle.NextLine:
					if (!isAtStartOfLine)
						NewLine();
					start = writer.GetLocation();
					writer.WriteToken(Roles.LBrace, "{", TextTokenType.Brace);
					end = writer.GetLocation();
					break;
				case BraceStyle.NextLineShifted:
					NewLine();
					writer.Indent();
					start = writer.GetLocation();
					writer.WriteToken(Roles.LBrace, "{", TextTokenType.Brace);
					end = writer.GetLocation();
					NewLine();
					return;
				case BraceStyle.NextLineShifted2:
					NewLine();
					writer.Indent();
					start = writer.GetLocation();
					writer.WriteToken(Roles.LBrace, "{", TextTokenType.Brace);
					end = writer.GetLocation();
					break;
				default:
					throw new ArgumentOutOfRangeException ();
			}
			writer.Indent();
			NewLine();
		}
		
		void CloseBrace(BraceStyle style, out TextLocation? start, out TextLocation? end)
		{
			switch (style) {
				case BraceStyle.DoNotChange:
				case BraceStyle.EndOfLine:
				case BraceStyle.EndOfLineWithoutSpace:
				case BraceStyle.NextLine:
					writer.Unindent();
					start = writer.GetLocation();
					writer.WriteToken(Roles.RBrace, "}", TextTokenType.Brace);
					end = writer.GetLocation();
					isAtStartOfLine = false;
					break;
				case BraceStyle.BannerStyle:
				case BraceStyle.NextLineShifted:
					start = writer.GetLocation();
					writer.WriteToken(Roles.RBrace, "}", TextTokenType.Brace);
					end = writer.GetLocation();
					isAtStartOfLine = false;
					writer.Unindent();
					break;
				case BraceStyle.NextLineShifted2:
					writer.Unindent();
					start = writer.GetLocation();
					writer.WriteToken(Roles.RBrace, "}", TextTokenType.Brace);
					end = writer.GetLocation();
					isAtStartOfLine = false;
					writer.Unindent();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion
		
		#region IsKeyword Test
		static readonly HashSet<string> unconditionalKeywords = new HashSet<string> {
			"abstract", "as", "base", "bool", "break", "byte", "case", "catch",
			"char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
			"do", "double", "else", "enum", "event", "explicit", "extern", "false",
			"finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
			"in", "int", "interface", "internal", "is", "lock", "long", "namespace",
			"new", "null", "object", "operator", "out", "override", "params", "private",
			"protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
			"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
			"true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
			"using", "virtual", "void", "volatile", "while"
		};
		static readonly HashSet<string> queryKeywords = new HashSet<string> {
			"from", "where", "join", "on", "equals", "into", "let", "orderby",
			"ascending", "descending", "select", "group", "by"
		};
		
		/// <summary>
		/// Determines whether the specified identifier is a keyword in the given context.
		/// </summary>
		public static bool IsKeyword(string identifier, AstNode context)
		{
			if (unconditionalKeywords.Contains(identifier)) {
				return true;
			}
			foreach (AstNode ancestor in context.Ancestors) {
				if (ancestor is QueryExpression && queryKeywords.Contains(identifier)) {
					return true;
				}
				if (identifier == "await") {
					// with lambdas/anonymous methods,
					if (ancestor is LambdaExpression) {
						return ((LambdaExpression)ancestor).IsAsync;
					}
					if (ancestor is AnonymousMethodExpression) {
						return ((AnonymousMethodExpression)ancestor).IsAsync;
					}
					if (ancestor is EntityDeclaration) {
						return (((EntityDeclaration)ancestor).Modifiers & Modifiers.Async) == Modifiers.Async;
					}
				}
			}
			return false;
		}
		#endregion
		
		#region Write constructs
		void WriteTypeArguments(IEnumerable<AstType> typeArguments)
		{
			if (typeArguments.Any()) {
				WriteToken(Roles.LChevron);
				WriteCommaSeparatedList(typeArguments);
				WriteToken(Roles.RChevron);
			}
		}
		
		public void WriteTypeParameters(IEnumerable<TypeParameterDeclaration> typeParameters)
		{
			if (typeParameters.Any()) {
				WriteToken(Roles.LChevron);
				WriteCommaSeparatedList(typeParameters);
				WriteToken(Roles.RChevron);
			}
		}
		
		void WriteModifiers(IEnumerable<CSharpModifierToken> modifierTokens)
		{
			foreach (CSharpModifierToken modifier in modifierTokens) {
				modifier.AcceptVisitor(this);
			}
		}
		
		void WriteQualifiedIdentifier(IEnumerable<Identifier> identifiers)
		{
			bool first = true;
			foreach (Identifier ident in identifiers) {
				if (first) {
					first = false;
				} else {
					writer.WriteTokenOperator(Roles.Dot, ".");
				}
				writer.WriteIdentifier(ident, TextTokenHelper.GetTextTokenType(ident.Annotation<object>()));
			}
		}
		
		void WriteEmbeddedStatement(Statement embeddedStatement)
		{
			if (embeddedStatement.IsNull) {
				NewLine();
				return;
			}
			BlockStatement block = embeddedStatement as BlockStatement;
			if (block != null) {
				VisitBlockStatement(block);
			} else {
				NewLine();
				writer.Indent();
				embeddedStatement.AcceptVisitor(this);
				writer.Unindent();
			}
		}
		
		void WriteMethodBody(BlockStatement body)
		{
			if (body.IsNull) {
				Semicolon();
			} else {
				VisitBlockStatement(body);
			}
		}
		
		void WriteAttributes(IEnumerable<AttributeSection> attributes)
		{
			foreach (AttributeSection attr in attributes) {
				attr.AcceptVisitor(this);
			}
		}
		
		void WritePrivateImplementationType(AstType privateImplementationType)
		{
			if (!privateImplementationType.IsNull) {
				privateImplementationType.AcceptVisitor(this);
				WriteToken(Roles.Dot);
			}
		}

		#endregion
		
		#region Expressions
		public void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
		{
			DebugExpression(anonymousMethodExpression);
			StartNode(anonymousMethodExpression);
			if (anonymousMethodExpression.IsAsync) {
				WriteKeyword(AnonymousMethodExpression.AsyncModifierRole);
				Space();
			}
			WriteKeyword(AnonymousMethodExpression.DelegateKeywordRole);
			if (anonymousMethodExpression.HasParameterList) {
				Space(policy.SpaceBeforeMethodDeclarationParentheses);
				WriteCommaSeparatedListInParenthesis(anonymousMethodExpression.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
			}
			anonymousMethodExpression.Body.AcceptVisitor(this);
			EndNode(anonymousMethodExpression);
		}
		
		public void VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression)
		{
			DebugExpression(undocumentedExpression);
			StartNode(undocumentedExpression);
			switch (undocumentedExpression.UndocumentedExpressionType) {
				case UndocumentedExpressionType.ArgList:
				case UndocumentedExpressionType.ArgListAccess:
					WriteKeyword(UndocumentedExpression.ArglistKeywordRole);
					break;
				case UndocumentedExpressionType.MakeRef:
					WriteKeyword(UndocumentedExpression.MakerefKeywordRole);
					break;
				case UndocumentedExpressionType.RefType:
					WriteKeyword(UndocumentedExpression.ReftypeKeywordRole);
					break;
				case UndocumentedExpressionType.RefValue:
					WriteKeyword(UndocumentedExpression.RefvalueKeywordRole);
					break;
			}
			if (undocumentedExpression.Arguments.Count > 0) {
				Space(policy.SpaceBeforeMethodCallParentheses);
				WriteCommaSeparatedListInParenthesis(undocumentedExpression.Arguments, policy.SpaceWithinMethodCallParentheses);
			}
			EndNode(undocumentedExpression);
		}
		
		public void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
		{
			DebugExpression(arrayCreateExpression);
			StartNode(arrayCreateExpression);
			WriteKeyword(ArrayCreateExpression.NewKeywordRole);
			arrayCreateExpression.Type.AcceptVisitor(this);
			if (arrayCreateExpression.Arguments.Count > 0) {
				WriteCommaSeparatedListInBrackets(arrayCreateExpression.Arguments);
			}
			foreach (var specifier in arrayCreateExpression.AdditionalArraySpecifiers) {
				specifier.AcceptVisitor(this);
			}
			arrayCreateExpression.Initializer.AcceptVisitor(this);
			EndNode(arrayCreateExpression);
		}
		
		public void VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
		{
			DebugExpression(arrayInitializerExpression);
			StartNode(arrayInitializerExpression);
			// "new List<int> { { 1 } }" and "new List<int> { 1 }" are the same semantically.
			// We also use the same AST for both: we always use two nested ArrayInitializerExpressions
			// for collection initializers, even if the user did not write nested brackets.
			// The output visitor will output nested braces only if they are necessary,
			// or if the braces tokens exist in the AST.
			bool bracesAreOptional = arrayInitializerExpression.Elements.Count == 1
				&& IsObjectOrCollectionInitializer(arrayInitializerExpression.Parent)
				&& !CanBeConfusedWithObjectInitializer(arrayInitializerExpression.Elements.Single());
			if (bracesAreOptional && arrayInitializerExpression.LBraceToken.IsNull) {
				arrayInitializerExpression.Elements.Single().AcceptVisitor(this);
			} else {
				PrintInitializerElements(arrayInitializerExpression.Elements);
			}
			EndNode(arrayInitializerExpression);
		}
		
		bool CanBeConfusedWithObjectInitializer(Expression expr)
		{
			// "int a; new List<int> { a = 1 };" is an object initalizers and invalid, but
			// "int a; new List<int> { { a = 1 } };" is a valid collection initializer.
			AssignmentExpression ae = expr as AssignmentExpression;
			return ae != null && ae.Operator == AssignmentOperatorType.Assign;
		}
		
		bool IsObjectOrCollectionInitializer(AstNode node)
		{
			if (!(node is ArrayInitializerExpression)) {
				return false;
			}
			if (node.Parent is ObjectCreateExpression) {
				return node.Role == ObjectCreateExpression.InitializerRole;
			}
			if (node.Parent is NamedExpression) {
				return node.Role == Roles.Expression;
			}
			return false;
		}
		
		void PrintInitializerElements(AstNodeCollection<Expression> elements)
		{
			BraceStyle style;
			if (policy.ArrayInitializerWrapping == Wrapping.WrapAlways) {
				style = BraceStyle.NextLine;
			} else {
				style = BraceStyle.EndOfLine;
			}
			OpenBrace(style);
			bool isFirst = true;
			AstNode last = null;
			foreach (AstNode node in elements) {
				if (isFirst) {
					isFirst = false;
				} else {
					Comma(node, noSpaceAfterComma: true);
					NewLine();
				}
				last = node;
				node.AcceptVisitor(this);
			}
			if (last != null)
				OptionalComma(last.NextSibling);
			NewLine();
			CloseBrace(style);
		}
		
		public void VisitAsExpression(AsExpression asExpression)
		{
			DebugExpression(asExpression);
			StartNode(asExpression);
			asExpression.Expression.AcceptVisitor(this);
			Space();
			WriteKeyword(AsExpression.AsKeywordRole);
			Space();
			asExpression.Type.AcceptVisitor(this);
			EndNode(asExpression);
		}
		
		public void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
		{
			DebugExpression(assignmentExpression);
			StartNode(assignmentExpression);
			assignmentExpression.Left.AcceptVisitor(this);
			Space(policy.SpaceAroundAssignment);
			WriteToken(AssignmentExpression.GetOperatorRole(assignmentExpression.Operator));
			Space(policy.SpaceAroundAssignment);
			assignmentExpression.Right.AcceptVisitor(this);
			EndNode(assignmentExpression);
		}
		
		public void VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
		{
			DebugExpression(baseReferenceExpression);
			StartNode(baseReferenceExpression);
			WriteKeyword("base", baseReferenceExpression.Role);
			EndNode(baseReferenceExpression);
		}
		
		public void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
		{
			DebugExpression(binaryOperatorExpression);
			StartNode(binaryOperatorExpression);
			binaryOperatorExpression.Left.AcceptVisitor(this);
			bool spacePolicy;
			switch (binaryOperatorExpression.Operator) {
				case BinaryOperatorType.BitwiseAnd:
				case BinaryOperatorType.BitwiseOr:
				case BinaryOperatorType.ExclusiveOr:
					spacePolicy = policy.SpaceAroundBitwiseOperator;
					break;
				case BinaryOperatorType.ConditionalAnd:
				case BinaryOperatorType.ConditionalOr:
					spacePolicy = policy.SpaceAroundLogicalOperator;
					break;
				case BinaryOperatorType.GreaterThan:
				case BinaryOperatorType.GreaterThanOrEqual:
				case BinaryOperatorType.LessThanOrEqual:
				case BinaryOperatorType.LessThan:
					spacePolicy = policy.SpaceAroundRelationalOperator;
					break;
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.InEquality:
					spacePolicy = policy.SpaceAroundEqualityOperator;
					break;
				case BinaryOperatorType.Add:
				case BinaryOperatorType.Subtract:
					spacePolicy = policy.SpaceAroundAdditiveOperator;
					break;
				case BinaryOperatorType.Multiply:
				case BinaryOperatorType.Divide:
				case BinaryOperatorType.Modulus:
					spacePolicy = policy.SpaceAroundMultiplicativeOperator;
					break;
				case BinaryOperatorType.ShiftLeft:
				case BinaryOperatorType.ShiftRight:
					spacePolicy = policy.SpaceAroundShiftOperator;
					break;
				case BinaryOperatorType.NullCoalescing:
					spacePolicy = true;
					break;
				default:
					throw new NotSupportedException ("Invalid value for BinaryOperatorType");
			}
			Space(spacePolicy);
			WriteToken(BinaryOperatorExpression.GetOperatorRole(binaryOperatorExpression.Operator));
			Space(spacePolicy);
			binaryOperatorExpression.Right.AcceptVisitor(this);
			EndNode(binaryOperatorExpression);
		}
		
		public void VisitCastExpression(CastExpression castExpression)
		{
			DebugExpression(castExpression);
			StartNode(castExpression);
			LPar();
			Space(policy.SpacesWithinCastParentheses);
			castExpression.Type.AcceptVisitor(this);
			Space(policy.SpacesWithinCastParentheses);
			RPar();
			Space(policy.SpaceAfterTypecast);
			castExpression.Expression.AcceptVisitor(this);
			EndNode(castExpression);
		}
		
		public void VisitCheckedExpression(CheckedExpression checkedExpression)
		{
			DebugExpression(checkedExpression);
			StartNode(checkedExpression);
			WriteKeyword(CheckedExpression.CheckedKeywordRole);
			LPar();
			Space(policy.SpacesWithinCheckedExpressionParantheses);
			checkedExpression.Expression.AcceptVisitor(this);
			Space(policy.SpacesWithinCheckedExpressionParantheses);
			RPar();
			EndNode(checkedExpression);
		}
		
		public void VisitConditionalExpression(ConditionalExpression conditionalExpression)
		{
			DebugExpression(conditionalExpression);
			StartNode(conditionalExpression);
			conditionalExpression.Condition.AcceptVisitor(this);
			
			Space(policy.SpaceBeforeConditionalOperatorCondition);
			WriteToken(ConditionalExpression.QuestionMarkRole);
			Space(policy.SpaceAfterConditionalOperatorCondition);
			
			conditionalExpression.TrueExpression.AcceptVisitor(this);
			
			Space(policy.SpaceBeforeConditionalOperatorSeparator);
			WriteToken(ConditionalExpression.ColonRole);
			Space(policy.SpaceAfterConditionalOperatorSeparator);
			
			conditionalExpression.FalseExpression.AcceptVisitor(this);
			
			EndNode(conditionalExpression);
		}
		
		public void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
		{
			DebugExpression(defaultValueExpression);
			StartNode(defaultValueExpression);
			
			WriteKeyword(DefaultValueExpression.DefaultKeywordRole);
			LPar();
			Space(policy.SpacesWithinTypeOfParentheses);
			defaultValueExpression.Type.AcceptVisitor(this);
			Space(policy.SpacesWithinTypeOfParentheses);
			RPar();
			
			EndNode(defaultValueExpression);
		}
		
		public void VisitDirectionExpression(DirectionExpression directionExpression)
		{
			DebugExpression(directionExpression);
			StartNode(directionExpression);
			
			switch (directionExpression.FieldDirection) {
				case FieldDirection.Out:
					WriteKeyword(DirectionExpression.OutKeywordRole);
					break;
				case FieldDirection.Ref:
					WriteKeyword(DirectionExpression.RefKeywordRole);
					break;
				default:
					throw new NotSupportedException ("Invalid value for FieldDirection");
			}
			Space();
			directionExpression.Expression.AcceptVisitor(this);
			
			EndNode(directionExpression);
		}
		
		public void VisitIdentifierExpression(IdentifierExpression identifierExpression)
		{
			DebugExpression(identifierExpression);
			StartNode(identifierExpression);
			WriteIdentifier(identifierExpression.IdentifierToken, TextTokenHelper.GetTextTokenType(identifierExpression.IdentifierToken.Annotation<object>()));
			WriteTypeArguments(identifierExpression.TypeArguments);
			EndNode(identifierExpression);
		}
		
		public void VisitIndexerExpression(IndexerExpression indexerExpression)
		{
			DebugExpression(indexerExpression);
			StartNode(indexerExpression);
			indexerExpression.Target.AcceptVisitor(this);
			Space(policy.SpaceBeforeMethodCallParentheses);
			WriteCommaSeparatedListInBrackets(indexerExpression.Arguments);
			EndNode(indexerExpression);
		}
		
		public void VisitInvocationExpression(InvocationExpression invocationExpression)
		{
			DebugExpression(invocationExpression);
			StartNode(invocationExpression);
			invocationExpression.Target.AcceptVisitor(this);
			Space(policy.SpaceBeforeMethodCallParentheses);
			WriteCommaSeparatedListInParenthesis(invocationExpression.Arguments, policy.SpaceWithinMethodCallParentheses);
			EndNode(invocationExpression);
		}
		
		public void VisitIsExpression(IsExpression isExpression)
		{
			DebugExpression(isExpression);
			StartNode(isExpression);
			isExpression.Expression.AcceptVisitor(this);
			Space();
			WriteKeyword(IsExpression.IsKeywordRole);
			isExpression.Type.AcceptVisitor(this);
			EndNode(isExpression);
		}
		
		public void VisitLambdaExpression(LambdaExpression lambdaExpression)
		{
			DebugExpression(lambdaExpression);
			StartNode(lambdaExpression);
			if (lambdaExpression.IsAsync) {
				WriteKeyword(LambdaExpression.AsyncModifierRole);
				Space();
			}
			if (LambdaNeedsParenthesis(lambdaExpression)) {
				WriteCommaSeparatedListInParenthesis(lambdaExpression.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
			} else {
				lambdaExpression.Parameters.Single().AcceptVisitor(this);
			}
			Space();
			WriteToken(LambdaExpression.ArrowRole);
			Space();
			lambdaExpression.Body.AcceptVisitor(this);
			EndNode(lambdaExpression);
		}
		
		bool LambdaNeedsParenthesis(LambdaExpression lambdaExpression)
		{
			if (lambdaExpression.Parameters.Count != 1) {
				return true;
			}
			var p = lambdaExpression.Parameters.Single();
			return !(p.Type.IsNull && p.ParameterModifier == ParameterModifier.None);
		}
		
		public void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
		{
			DebugExpression(memberReferenceExpression);
			StartNode(memberReferenceExpression);
			memberReferenceExpression.Target.AcceptVisitor(this);
			WriteToken(Roles.Dot);
			WriteIdentifier(memberReferenceExpression.MemberNameToken, TextTokenHelper.GetTextTokenType(memberReferenceExpression.MemberNameToken.Annotation<object>() ?? memberReferenceExpression.Annotation<object>()));
			WriteTypeArguments(memberReferenceExpression.TypeArguments);
			EndNode(memberReferenceExpression);
		}
		
		public void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
		{
			DebugExpression(namedArgumentExpression);
			StartNode(namedArgumentExpression);
			WriteIdentifier(namedArgumentExpression.NameToken);
			WriteToken(Roles.Colon);
			Space();
			namedArgumentExpression.Expression.AcceptVisitor(this);
			EndNode(namedArgumentExpression);
		}
		
		public void VisitNamedExpression(NamedExpression namedExpression)
		{
			DebugExpression(namedExpression);
			StartNode(namedExpression);
			WriteIdentifier(namedExpression.NameToken);
			Space();
			WriteToken(Roles.Assign);
			Space();
			namedExpression.Expression.AcceptVisitor(this);
			EndNode(namedExpression);
		}
		
		public void VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
		{
			DebugExpression(nullReferenceExpression);
			StartNode(nullReferenceExpression);
			writer.WritePrimitiveValue(null);
			EndNode(nullReferenceExpression);
		}
		
		public void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
		{
			DebugExpression(objectCreateExpression);
			StartNode(objectCreateExpression);
			WriteKeyword(ObjectCreateExpression.NewKeywordRole);
			objectCreateExpression.Type.AcceptVisitor(this);
			bool useParenthesis = objectCreateExpression.Arguments.Any() || objectCreateExpression.Initializer.IsNull;
			// also use parenthesis if there is an '(' token
			if (!objectCreateExpression.LParToken.IsNull) {
				useParenthesis = true;
			}
			if (useParenthesis) {
				Space(policy.SpaceBeforeMethodCallParentheses);
				WriteCommaSeparatedListInParenthesis(objectCreateExpression.Arguments, policy.SpaceWithinMethodCallParentheses);
			}
			objectCreateExpression.Initializer.AcceptVisitor(this);
			EndNode(objectCreateExpression);
		}
		
		public void VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
		{
			DebugExpression(anonymousTypeCreateExpression);
			StartNode(anonymousTypeCreateExpression);
			WriteKeyword(AnonymousTypeCreateExpression.NewKeywordRole);
			PrintInitializerElements(anonymousTypeCreateExpression.Initializers);
			EndNode(anonymousTypeCreateExpression);
		}

		public void VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
		{
			DebugExpression(parenthesizedExpression);
			StartNode(parenthesizedExpression);
			LPar();
			Space(policy.SpacesWithinParentheses);
			parenthesizedExpression.Expression.AcceptVisitor(this);
			Space(policy.SpacesWithinParentheses);
			RPar();
			EndNode(parenthesizedExpression);
		}
		
		public void VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
		{
			DebugExpression(pointerReferenceExpression);
			StartNode(pointerReferenceExpression);
			pointerReferenceExpression.Target.AcceptVisitor(this);
			WriteToken(PointerReferenceExpression.ArrowRole);
			WriteIdentifier(pointerReferenceExpression.MemberNameToken, TextTokenHelper.GetTextTokenType(pointerReferenceExpression.MemberNameToken.Annotation<object>()));
			WriteTypeArguments(pointerReferenceExpression.TypeArguments);
			EndNode(pointerReferenceExpression);
		}
		
		#region VisitPrimitiveExpression
		public void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
		{
			DebugExpression(primitiveExpression);
			StartNode(primitiveExpression);
			writer.WritePrimitiveValue(primitiveExpression.Value, TextTokenType.Text, primitiveExpression.UnsafeLiteralValue);
			EndNode(primitiveExpression);
		}
		#endregion
		
		public void VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
		{
			DebugExpression(sizeOfExpression);
			StartNode(sizeOfExpression);
			
			WriteKeyword(SizeOfExpression.SizeofKeywordRole);
			LPar();
			Space(policy.SpacesWithinSizeOfParentheses);
			sizeOfExpression.Type.AcceptVisitor(this);
			Space(policy.SpacesWithinSizeOfParentheses);
			RPar();
			
			EndNode(sizeOfExpression);
		}
		
		public void VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
		{
			DebugExpression(stackAllocExpression);
			StartNode(stackAllocExpression);
			WriteKeyword(StackAllocExpression.StackallocKeywordRole);
			stackAllocExpression.Type.AcceptVisitor(this);
			WriteCommaSeparatedListInBrackets(new[] { stackAllocExpression.CountExpression });
			EndNode(stackAllocExpression);
		}
		
		public void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
		{
			DebugExpression(thisReferenceExpression);
			StartNode(thisReferenceExpression);
			WriteKeyword("this", thisReferenceExpression.Role);
			EndNode(thisReferenceExpression);
		}
		
		public void VisitTypeOfExpression(TypeOfExpression typeOfExpression)
		{
			DebugExpression(typeOfExpression);
			StartNode(typeOfExpression);
			
			WriteKeyword(TypeOfExpression.TypeofKeywordRole);
			LPar();
			Space(policy.SpacesWithinTypeOfParentheses);
			typeOfExpression.Type.AcceptVisitor(this);
			Space(policy.SpacesWithinTypeOfParentheses);
			RPar();
			
			EndNode(typeOfExpression);
		}
		
		public void VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression)
		{
			DebugExpression(typeReferenceExpression);
			StartNode(typeReferenceExpression);
			typeReferenceExpression.Type.AcceptVisitor(this);
			EndNode(typeReferenceExpression);
		}
		
		public void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
		{
			DebugExpression(unaryOperatorExpression);
			StartNode(unaryOperatorExpression);
			UnaryOperatorType opType = unaryOperatorExpression.Operator;
			var opSymbol = UnaryOperatorExpression.GetOperatorRole(opType);
			if (opType == UnaryOperatorType.Await) {
				WriteKeyword(opSymbol);
			} else if (!(opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement)) {
				WriteToken(opSymbol);
			}
			unaryOperatorExpression.Expression.AcceptVisitor(this);
			if (opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement) {
				WriteToken(opSymbol);
			}
			EndNode(unaryOperatorExpression);
		}
		
		public void VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
		{
			DebugExpression(uncheckedExpression);
			StartNode(uncheckedExpression);
			WriteKeyword(UncheckedExpression.UncheckedKeywordRole);
			LPar();
			Space(policy.SpacesWithinCheckedExpressionParantheses);
			uncheckedExpression.Expression.AcceptVisitor(this);
			Space(policy.SpacesWithinCheckedExpressionParantheses);
			RPar();
			EndNode(uncheckedExpression);
		}

		#endregion
		
		#region Query Expressions
		public void VisitQueryExpression(QueryExpression queryExpression)
		{
			DebugExpression(queryExpression);
			StartNode(queryExpression);
			bool indent = queryExpression.Parent is QueryClause && !(queryExpression.Parent is QueryContinuationClause);
			if (indent) {
				writer.Indent();
				NewLine();
			}
			bool first = true;
			foreach (var clause in queryExpression.Clauses) {
				if (first) {
					first = false;
				} else {
					if (!(clause is QueryContinuationClause)) {
						NewLine();
					}
				}
				clause.AcceptVisitor(this);
			}
			if (indent) {
				writer.Unindent();
			}
			EndNode(queryExpression);
		}
		
		public void VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause)
		{
			DebugExpression(queryContinuationClause);
			StartNode(queryContinuationClause);
			queryContinuationClause.PrecedingQuery.AcceptVisitor(this);
			Space();
			WriteKeyword(QueryContinuationClause.IntoKeywordRole);
			Space();
			WriteIdentifier(queryContinuationClause.IdentifierToken);
			EndNode(queryContinuationClause);
		}
		
		public void VisitQueryFromClause(QueryFromClause queryFromClause)
		{
			DebugExpression(queryFromClause);
			StartNode(queryFromClause);
			WriteKeyword(QueryFromClause.FromKeywordRole);
			queryFromClause.Type.AcceptVisitor(this);
			Space();
			WriteIdentifier(queryFromClause.IdentifierToken);
			Space();
			WriteKeyword(QueryFromClause.InKeywordRole);
			Space();
			queryFromClause.Expression.AcceptVisitor(this);
			EndNode(queryFromClause);
		}
		
		public void VisitQueryLetClause(QueryLetClause queryLetClause)
		{
			DebugExpression(queryLetClause);
			StartNode(queryLetClause);
			WriteKeyword(QueryLetClause.LetKeywordRole);
			Space();
			WriteIdentifier(queryLetClause.IdentifierToken);
			Space(policy.SpaceAroundAssignment);
			WriteToken(Roles.Assign);
			Space(policy.SpaceAroundAssignment);
			queryLetClause.Expression.AcceptVisitor(this);
			EndNode(queryLetClause);
		}
		
		public void VisitQueryWhereClause(QueryWhereClause queryWhereClause)
		{
			DebugExpression(queryWhereClause);
			StartNode(queryWhereClause);
			WriteKeyword(QueryWhereClause.WhereKeywordRole);
			Space();
			queryWhereClause.Condition.AcceptVisitor(this);
			EndNode(queryWhereClause);
		}
		
		public void VisitQueryJoinClause(QueryJoinClause queryJoinClause)
		{
			DebugExpression(queryJoinClause);
			StartNode(queryJoinClause);
			WriteKeyword(QueryJoinClause.JoinKeywordRole);
			queryJoinClause.Type.AcceptVisitor(this);
			Space();
			WriteIdentifier(queryJoinClause.JoinIdentifierToken, TextTokenHelper.GetTextTokenType(queryJoinClause.JoinIdentifierToken.Annotation<object>()));
			Space();
			WriteKeyword(QueryJoinClause.InKeywordRole);
			Space();
			queryJoinClause.InExpression.AcceptVisitor(this);
			Space();
			WriteKeyword(QueryJoinClause.OnKeywordRole);
			Space();
			queryJoinClause.OnExpression.AcceptVisitor(this);
			Space();
			WriteKeyword(QueryJoinClause.EqualsKeywordRole);
			Space();
			queryJoinClause.EqualsExpression.AcceptVisitor(this);
			if (queryJoinClause.IsGroupJoin) {
				Space();
				WriteKeyword(QueryJoinClause.IntoKeywordRole);
				WriteIdentifier(queryJoinClause.IntoIdentifierToken, TextTokenHelper.GetTextTokenType(queryJoinClause.IntoIdentifierToken.Annotation<object>()));
			}
			EndNode(queryJoinClause);
		}
		
		public void VisitQueryOrderClause(QueryOrderClause queryOrderClause)
		{
			DebugExpression(queryOrderClause);
			StartNode(queryOrderClause);
			WriteKeyword(QueryOrderClause.OrderbyKeywordRole);
			Space();
			WriteCommaSeparatedList(queryOrderClause.Orderings);
			EndNode(queryOrderClause);
		}
		
		public void VisitQueryOrdering(QueryOrdering queryOrdering)
		{
			DebugExpression(queryOrdering);
			StartNode(queryOrdering);
			queryOrdering.Expression.AcceptVisitor(this);
			switch (queryOrdering.Direction) {
				case QueryOrderingDirection.Ascending:
					Space();
					WriteKeyword(QueryOrdering.AscendingKeywordRole);
					break;
				case QueryOrderingDirection.Descending:
					Space();
					WriteKeyword(QueryOrdering.DescendingKeywordRole);
					break;
			}
			EndNode(queryOrdering);
		}
		
		public void VisitQuerySelectClause(QuerySelectClause querySelectClause)
		{
			DebugExpression(querySelectClause);
			StartNode(querySelectClause);
			WriteKeyword(QuerySelectClause.SelectKeywordRole);
			Space();
			querySelectClause.Expression.AcceptVisitor(this);
			EndNode(querySelectClause);
		}
		
		public void VisitQueryGroupClause(QueryGroupClause queryGroupClause)
		{
			DebugExpression(queryGroupClause);
			StartNode(queryGroupClause);
			WriteKeyword(QueryGroupClause.GroupKeywordRole);
			Space();
			queryGroupClause.Projection.AcceptVisitor(this);
			Space();
			WriteKeyword(QueryGroupClause.ByKeywordRole);
			Space();
			queryGroupClause.Key.AcceptVisitor(this);
			EndNode(queryGroupClause);
		}

		#endregion
		
		#region GeneralScope
		public void VisitAttribute(Attribute attribute)
		{
			StartNode(attribute);
			attribute.Type.AcceptVisitor(this);
			if (attribute.Arguments.Count != 0 || !attribute.GetChildByRole(Roles.LPar).IsNull) {
				Space(policy.SpaceBeforeMethodCallParentheses);
				WriteCommaSeparatedListInParenthesis(attribute.Arguments, policy.SpaceWithinMethodCallParentheses);
			}
			EndNode(attribute);
		}
		
		public void VisitAttributeSection(AttributeSection attributeSection)
		{
			StartNode(attributeSection);
			WriteToken(Roles.LBracket);
			if (!string.IsNullOrEmpty(attributeSection.AttributeTarget)) {
				WriteKeyword(attributeSection.AttributeTarget, Roles.Identifier);
				WriteToken(Roles.Colon);
				Space();
			}
			WriteCommaSeparatedList(attributeSection.Attributes);
			WriteToken(Roles.RBracket);
			if (attributeSection.Parent is ParameterDeclaration || attributeSection.Parent is TypeParameterDeclaration) {
				Space();
			} else {
				NewLine();
			}
			EndNode(attributeSection);
		}
		
		public void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
		{
			StartNode(delegateDeclaration);
			WriteAttributes(delegateDeclaration.Attributes);
			WriteModifiers(delegateDeclaration.ModifierTokens);
			WriteKeyword(Roles.DelegateKeyword);
			delegateDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WriteIdentifier(delegateDeclaration.NameToken);
			WriteTypeParameters(delegateDeclaration.TypeParameters);
			Space(policy.SpaceBeforeDelegateDeclarationParentheses);
			WriteCommaSeparatedListInParenthesis(delegateDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
			foreach (Constraint constraint in delegateDeclaration.Constraints) {
				constraint.AcceptVisitor(this);
			}
			Semicolon();
			EndNode(delegateDeclaration);
		}
		
		public void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
		{
			StartNode(namespaceDeclaration);
			WriteKeyword(Roles.NamespaceKeyword);
			namespaceDeclaration.NamespaceName.AcceptVisitor (this);
			OpenBrace(policy.NamespaceBraceStyle);
			foreach (var member in namespaceDeclaration.Members) {
				member.AcceptVisitor(this);
				MaybeNewLinesAfterUsings(member);
			}
			CloseBrace(policy.NamespaceBraceStyle);
			OptionalSemicolon(namespaceDeclaration.LastChild);
			NewLine();
			EndNode(namespaceDeclaration);
		}
		
		public void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
		{
			StartNode(typeDeclaration);
			WriteAttributes(typeDeclaration.Attributes);
			WriteModifiers(typeDeclaration.ModifierTokens);
			BraceStyle braceStyle;
			switch (typeDeclaration.ClassType) {
				case ClassType.Enum:
					WriteKeyword(Roles.EnumKeyword);
					braceStyle = policy.EnumBraceStyle;
					break;
				case ClassType.Interface:
					WriteKeyword(Roles.InterfaceKeyword);
					braceStyle = policy.InterfaceBraceStyle;
					break;
				case ClassType.Struct:
					WriteKeyword(Roles.StructKeyword);
					braceStyle = policy.StructBraceStyle;
					break;
				default:
					WriteKeyword(Roles.ClassKeyword);
					braceStyle = policy.ClassBraceStyle;
					break;
			}
			WriteIdentifier(typeDeclaration.NameToken);
			WriteTypeParameters(typeDeclaration.TypeParameters);
			if (typeDeclaration.BaseTypes.Any()) {
				Space();
				WriteToken(Roles.Colon);
				Space();
				WriteCommaSeparatedList(typeDeclaration.BaseTypes);
			}
			foreach (Constraint constraint in typeDeclaration.Constraints) {
				constraint.AcceptVisitor(this);
			}
			OpenBrace(braceStyle);
			if (typeDeclaration.ClassType == ClassType.Enum) {
				bool first = true;
				AstNode last = null;
				foreach (var member in typeDeclaration.Members) {
					if (first) {
						first = false;
					} else {
						Comma(member, noSpaceAfterComma: true);
						NewLine();
					}
					last = member;
					member.AcceptVisitor(this);
				}
				if (last != null)
					OptionalComma(last.NextSibling);
				NewLine();
			} else {
				bool first = true;
				foreach (var member in typeDeclaration.Members) {
					if (!first) {
						for (int i = 0; i < policy.MinimumBlankLinesBetweenMembers; i++)
							NewLine();
					}
					first = false;
					member.AcceptVisitor(this);
				}
			}
			CloseBrace(braceStyle);
			OptionalSemicolon(typeDeclaration.LastChild);
			NewLine();
			EndNode(typeDeclaration);
		}
		
		public void VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration)
		{
			StartNode(usingAliasDeclaration);
			WriteKeyword(UsingAliasDeclaration.UsingKeywordRole);
			WriteIdentifier(usingAliasDeclaration.GetChildByRole(UsingAliasDeclaration.AliasRole), TextTokenType.Text);
			Space(policy.SpaceAroundEqualityOperator);
			WriteToken(Roles.Assign);
			Space(policy.SpaceAroundEqualityOperator);
			usingAliasDeclaration.Import.AcceptVisitor(this);
			Semicolon();
			EndNode(usingAliasDeclaration);
		}
		
		public void VisitUsingDeclaration(UsingDeclaration usingDeclaration)
		{
			StartNode(usingDeclaration);
			WriteKeyword(UsingDeclaration.UsingKeywordRole);
			usingDeclaration.Import.AcceptVisitor(this);
			Semicolon();
			EndNode(usingDeclaration);
		}
		
		public void VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
		{
			StartNode(externAliasDeclaration);
			WriteKeyword(Roles.ExternKeyword);
			Space();
			WriteKeyword(Roles.AliasKeyword);
			Space();
			WriteIdentifier(externAliasDeclaration.NameToken);
			Semicolon();
			EndNode(externAliasDeclaration);
		}

		#endregion
		
		#region Statements
		public void VisitBlockStatement(BlockStatement blockStatement)
		{
			StartNode(blockStatement);
			BraceStyle style;
			if (blockStatement.Parent is AnonymousMethodExpression || blockStatement.Parent is LambdaExpression) {
				style = policy.AnonymousMethodBraceStyle;
			} else if (blockStatement.Parent is ConstructorDeclaration) {
				style = policy.ConstructorBraceStyle;
			} else if (blockStatement.Parent is DestructorDeclaration) {
				style = policy.DestructorBraceStyle;
			} else if (blockStatement.Parent is MethodDeclaration) {
				style = policy.MethodBraceStyle;
			} else if (blockStatement.Parent is Accessor) {
				if (blockStatement.Parent.Role == PropertyDeclaration.GetterRole) {
					style = policy.PropertyGetBraceStyle;
				} else if (blockStatement.Parent.Role == PropertyDeclaration.SetterRole) {
					style = policy.PropertySetBraceStyle;
				} else if (blockStatement.Parent.Role == CustomEventDeclaration.AddAccessorRole) {
					style = policy.EventAddBraceStyle;
				} else if (blockStatement.Parent.Role == CustomEventDeclaration.RemoveAccessorRole) {
					style = policy.EventRemoveBraceStyle;
				} else {
					style = policy.StatementBraceStyle;
				}
			} else {
				style = policy.StatementBraceStyle;
			}
			TextLocation? start, end;
			OpenBrace(style, out start, out end);
			if (blockStatement.HiddenStart != null) {
				DebugStart(blockStatement, start);
				DebugHidden(blockStatement.HiddenStart);
				DebugEnd(blockStatement, end);
			}
			foreach (var node in blockStatement.Statements) {
				node.AcceptVisitor(this);
			}
			EndNode(blockStatement);
			CloseBrace(style, out start, out end);
			if (blockStatement.HiddenEnd != null) {
				DebugStart(blockStatement, start);
				DebugHidden(blockStatement.HiddenEnd);
				DebugEnd(blockStatement, end);
			}
			if (!(blockStatement.Parent is Expression))
				NewLine();
		}
		
		public void VisitBreakStatement(BreakStatement breakStatement)
		{
			StartNode(breakStatement);
			DebugStart(breakStatement);
			WriteKeyword("break", BreakStatement.BreakKeywordRole);
			SemicolonDebugEnd(breakStatement);
			EndNode(breakStatement);
		}
		
		public void VisitCheckedStatement(CheckedStatement checkedStatement)
		{
			DebugExpression(checkedStatement);
			StartNode(checkedStatement);
			WriteKeyword(CheckedStatement.CheckedKeywordRole);
			checkedStatement.Body.AcceptVisitor(this);
			EndNode(checkedStatement);
		}
		
		public void VisitContinueStatement(ContinueStatement continueStatement)
		{
			StartNode(continueStatement);
			DebugStart(continueStatement);
			WriteKeyword("continue", ContinueStatement.ContinueKeywordRole);
			SemicolonDebugEnd(continueStatement);
			EndNode(continueStatement);
		}
		
		public void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
		{
			StartNode(doWhileStatement);
			WriteKeyword(DoWhileStatement.DoKeywordRole);
			WriteEmbeddedStatement(doWhileStatement.EmbeddedStatement);
			DebugStart(doWhileStatement);
			WriteKeyword(DoWhileStatement.WhileKeywordRole);
			Space(policy.SpaceBeforeWhileParentheses);
			LPar();
			Space(policy.SpacesWithinWhileParentheses);
			doWhileStatement.Condition.AcceptVisitor(this);
			Space(policy.SpacesWithinWhileParentheses);
			RPar();
			SemicolonDebugEnd(doWhileStatement);
			EndNode(doWhileStatement);
		}
		
		public void VisitEmptyStatement(EmptyStatement emptyStatement)
		{
			DebugExpression(emptyStatement);
			StartNode(emptyStatement);
			Semicolon();
			EndNode(emptyStatement);
		}
		
		public void VisitExpressionStatement(ExpressionStatement expressionStatement)
		{
			StartNode(expressionStatement);
			DebugStart(expressionStatement);
			expressionStatement.Expression.AcceptVisitor(this);
			SemicolonDebugEnd(expressionStatement);
			EndNode(expressionStatement);
		}
		
		public void VisitFixedStatement(FixedStatement fixedStatement)
		{
			StartNode(fixedStatement);
			WriteKeyword(FixedStatement.FixedKeywordRole);
			Space(policy.SpaceBeforeUsingParentheses);
			LPar();
			Space(policy.SpacesWithinUsingParentheses);
			DebugStart(fixedStatement);
			fixedStatement.Type.AcceptVisitor(this);
			Space();
			WriteCommaSeparatedList(fixedStatement.Variables);
			DebugEnd(fixedStatement);
			Space(policy.SpacesWithinUsingParentheses);
			RPar();
			WriteEmbeddedStatement(fixedStatement.EmbeddedStatement);
			EndNode(fixedStatement);
		}
		
		public void VisitForeachStatement(ForeachStatement foreachStatement)
		{
			StartNode(foreachStatement);
			WriteKeyword(ForeachStatement.ForeachKeywordRole);
			Space(policy.SpaceBeforeForeachParentheses);
			LPar();
			Space(policy.SpacesWithinForeachParentheses);
			DebugStart(foreachStatement);
			foreachStatement.VariableType.AcceptVisitor(this);
			Space();
			WriteIdentifier(foreachStatement.VariableNameToken);
			DebugHidden(foreachStatement.HiddenGetCurrentNode);
			DebugEnd(foreachStatement, false);
			Space();
			DebugStart(foreachStatement);
			WriteKeyword(ForeachStatement.InKeywordRole);
			DebugHidden(foreachStatement.HiddenMoveNextNode);
			DebugEnd(foreachStatement, false);
			Space();
			DebugStart(foreachStatement);
			foreachStatement.InExpression.AcceptVisitor(this);
			DebugHidden(foreachStatement.HiddenGetEnumeratorNode);
			DebugEnd(foreachStatement, false);
			Space(policy.SpacesWithinForeachParentheses);
			RPar();
			WriteEmbeddedStatement(foreachStatement.EmbeddedStatement);
			EndNode(foreachStatement);
		}
		
		public void VisitForStatement(ForStatement forStatement)
		{
			StartNode(forStatement);
			WriteKeyword(ForStatement.ForKeywordRole);
			Space(policy.SpaceBeforeForParentheses);
			LPar();
			Space(policy.SpacesWithinForParentheses);
			
			DebugStart(forStatement);
			WriteCommaSeparatedList(forStatement.Initializers);
			Space(policy.SpaceBeforeForSemicolon);
			WriteToken(Roles.Semicolon);
			DebugEnd(forStatement, false);
			Space(policy.SpaceAfterForSemicolon);
			
			DebugStart(forStatement);
			forStatement.Condition.AcceptVisitor(this);
			DebugEnd(forStatement, false);
			Space(policy.SpaceBeforeForSemicolon);
			WriteToken(Roles.Semicolon);
			if (forStatement.Iterators.Any()) {
				Space(policy.SpaceAfterForSemicolon);
				DebugStart(forStatement);
				WriteCommaSeparatedList(forStatement.Iterators);
				DebugEnd(forStatement, false);
			}
			
			Space(policy.SpacesWithinForParentheses);
			RPar();
			WriteEmbeddedStatement(forStatement.EmbeddedStatement);
			EndNode(forStatement);
		}
		
		public void VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement)
		{
			StartNode(gotoCaseStatement);
			DebugStart(gotoCaseStatement);
			WriteKeyword(GotoCaseStatement.GotoKeywordRole);
			WriteKeyword(GotoCaseStatement.CaseKeywordRole);
			Space();
			gotoCaseStatement.LabelExpression.AcceptVisitor(this);
			SemicolonDebugEnd(gotoCaseStatement);
			EndNode(gotoCaseStatement);
		}
		
		public void VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement)
		{
			StartNode(gotoDefaultStatement);
			DebugStart(gotoDefaultStatement);
			WriteKeyword(GotoDefaultStatement.GotoKeywordRole);
			WriteKeyword(GotoDefaultStatement.DefaultKeywordRole);
			SemicolonDebugEnd(gotoDefaultStatement);
			EndNode(gotoDefaultStatement);
		}
		
		public void VisitGotoStatement(GotoStatement gotoStatement)
		{
			StartNode(gotoStatement);
			DebugStart(gotoStatement);
			WriteKeyword(GotoStatement.GotoKeywordRole);
			WriteIdentifier(gotoStatement.GetChildByRole(Roles.Identifier), TextTokenType.Label);
			SemicolonDebugEnd(gotoStatement);
			EndNode(gotoStatement);
		}
		
		public void VisitIfElseStatement(IfElseStatement ifElseStatement)
		{
			StartNode(ifElseStatement);
			DebugStart(ifElseStatement, IfElseStatement.IfKeywordRole);
			Space(policy.SpaceBeforeIfParentheses);
			LPar();
			Space(policy.SpacesWithinIfParentheses);
			ifElseStatement.Condition.AcceptVisitor(this);
			Space(policy.SpacesWithinIfParentheses);
			RPar();
			DebugEnd(ifElseStatement);
			WriteEmbeddedStatement(ifElseStatement.TrueStatement);
			if (!ifElseStatement.FalseStatement.IsNull) {
				WriteKeyword(IfElseStatement.ElseKeywordRole);
				if (ifElseStatement.FalseStatement is IfElseStatement) {
					// don't put newline between 'else' and 'if'
					ifElseStatement.FalseStatement.AcceptVisitor(this);
				} else {
					WriteEmbeddedStatement(ifElseStatement.FalseStatement);
				}
			}
			EndNode(ifElseStatement);
		}
		
		public void VisitLabelStatement(LabelStatement labelStatement)
		{
			DebugExpression(labelStatement);
			StartNode(labelStatement);
			WriteIdentifier(labelStatement.GetChildByRole(Roles.Identifier), TextTokenType.Label);
			WriteToken(Roles.Colon);
			bool foundLabelledStatement = false;
			for (AstNode tmp = labelStatement.NextSibling; tmp != null; tmp = tmp.NextSibling) {
				if (tmp.Role == labelStatement.Role) {
					foundLabelledStatement = true;
				}
			}
			if (!foundLabelledStatement) {
				// introduce an EmptyStatement so that the output becomes syntactically valid
				WriteToken(Roles.Semicolon);
			}
			NewLine();
			EndNode(labelStatement);
		}
		
		public void VisitLockStatement(LockStatement lockStatement)
		{
			StartNode(lockStatement);
			DebugStart(lockStatement);
			WriteKeyword(LockStatement.LockKeywordRole);
			Space(policy.SpaceBeforeLockParentheses);
			LPar();
			Space(policy.SpacesWithinLockParentheses);
			lockStatement.Expression.AcceptVisitor(this);
			Space(policy.SpacesWithinLockParentheses);
			RPar();
			DebugEnd(lockStatement);
			WriteEmbeddedStatement(lockStatement.EmbeddedStatement);
			EndNode(lockStatement);
		}
		
		public void VisitReturnStatement(ReturnStatement returnStatement)
		{
			StartNode(returnStatement);
			DebugStart(returnStatement);
			WriteKeyword(ReturnStatement.ReturnKeywordRole);
			if (!returnStatement.Expression.IsNull) {
				Space();
				returnStatement.Expression.AcceptVisitor(this);
			}
			SemicolonDebugEnd(returnStatement);
			EndNode(returnStatement);
		}
		
		public void VisitSwitchStatement(SwitchStatement switchStatement)
		{
			StartNode(switchStatement);
			DebugStart(switchStatement);
			WriteKeyword(SwitchStatement.SwitchKeywordRole);
			Space(policy.SpaceBeforeSwitchParentheses);
			LPar();
			Space(policy.SpacesWithinSwitchParentheses);
			switchStatement.Expression.AcceptVisitor(this);
			Space(policy.SpacesWithinSwitchParentheses);
			RPar();
			DebugEnd(switchStatement);
			OpenBrace(policy.StatementBraceStyle);
			if (!policy.IndentSwitchBody) {
				writer.Unindent();
			}
			
			foreach (var section in switchStatement.SwitchSections) {
				section.AcceptVisitor(this);
			}
			
			if (!policy.IndentSwitchBody) {
				writer.Indent();
			}
			TextLocation? start, end;
			CloseBrace(policy.StatementBraceStyle, out start, out end);
			if (switchStatement.HiddenEnd != null) {
				DebugStart(switchStatement, start);
				DebugHidden(switchStatement.HiddenEnd);
				DebugEnd(switchStatement, end);
			}
			NewLine();
			EndNode(switchStatement);
		}
		
		public void VisitSwitchSection(SwitchSection switchSection)
		{
			StartNode(switchSection);
			bool first = true;
			foreach (var label in switchSection.CaseLabels) {
				if (!first) {
					NewLine();
				}
				label.AcceptVisitor(this);
				first = false;
			}
			bool isBlock = switchSection.Statements.Count == 1 && switchSection.Statements.Single() is BlockStatement;
			if (policy.IndentCaseBody && !isBlock) {
				writer.Indent();
			}
			
			if (!isBlock)
				NewLine();
			
			foreach (var statement in switchSection.Statements) {
				statement.AcceptVisitor(this);
			}
			
			if (policy.IndentCaseBody && !isBlock) {
				writer.Unindent();
			}
			
			EndNode(switchSection);
		}
		
		public void VisitCaseLabel(CaseLabel caseLabel)
		{
			DebugExpression(caseLabel);
			StartNode(caseLabel);
			if (caseLabel.Expression.IsNull) {
				WriteKeyword(CaseLabel.DefaultKeywordRole);
			} else {
				WriteKeyword(CaseLabel.CaseKeywordRole);
				Space();
				caseLabel.Expression.AcceptVisitor(this);
			}
			WriteToken(Roles.Colon);
			EndNode(caseLabel);
		}
		
		public void VisitThrowStatement(ThrowStatement throwStatement)
		{
			StartNode(throwStatement);
			DebugStart(throwStatement);
			WriteKeyword(ThrowStatement.ThrowKeywordRole);
			if (!throwStatement.Expression.IsNull) {
				Space();
				throwStatement.Expression.AcceptVisitor(this);
			}
			SemicolonDebugEnd(throwStatement);
			EndNode(throwStatement);
		}
		
		public void VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
		{
			StartNode(tryCatchStatement);
			WriteKeyword(TryCatchStatement.TryKeywordRole);
			tryCatchStatement.TryBlock.AcceptVisitor(this);
			foreach (var catchClause in tryCatchStatement.CatchClauses) {
				catchClause.AcceptVisitor(this);
			}
			if (!tryCatchStatement.FinallyBlock.IsNull) {
				WriteKeyword(TryCatchStatement.FinallyKeywordRole);
				tryCatchStatement.FinallyBlock.AcceptVisitor(this);
			}
			EndNode(tryCatchStatement);
		}
		
		public void VisitCatchClause(CatchClause catchClause)
		{
			StartNode(catchClause);
			DebugStart(catchClause);
			WriteKeyword(CatchClause.CatchKeywordRole);
			if (!catchClause.Type.IsNull) {
				Space(policy.SpaceBeforeCatchParentheses);
				LPar();
				Space(policy.SpacesWithinCatchParentheses);
				catchClause.Type.AcceptVisitor(this);
				if (!string.IsNullOrEmpty(catchClause.VariableName)) {
					Space();
					WriteIdentifier(catchClause.VariableNameToken);
				}
				Space(policy.SpacesWithinCatchParentheses);
				RPar();
			}
			DebugEnd(catchClause);
			if (!catchClause.Condition.IsNull) {
				Space();
				WriteKeyword(CatchClause.WhenKeywordRole);
				Space(policy.SpaceBeforeIfParentheses);
				LPar();
				Space(policy.SpacesWithinIfParentheses);
				catchClause.Condition.AcceptVisitor(this);
				Space(policy.SpacesWithinIfParentheses);
				RPar();
			}
			catchClause.Body.AcceptVisitor(this);
			EndNode(catchClause);
		}
		
		public void VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
		{
			DebugExpression(uncheckedStatement);
			StartNode(uncheckedStatement);
			WriteKeyword(UncheckedStatement.UncheckedKeywordRole);
			uncheckedStatement.Body.AcceptVisitor(this);
			EndNode(uncheckedStatement);
		}
		
		public void VisitUnsafeStatement(UnsafeStatement unsafeStatement)
		{
			DebugExpression(unsafeStatement);
			StartNode(unsafeStatement);
			WriteKeyword(UnsafeStatement.UnsafeKeywordRole);
			unsafeStatement.Body.AcceptVisitor(this);
			EndNode(unsafeStatement);
		}
		
		public void VisitUsingStatement(UsingStatement usingStatement)
		{
			StartNode(usingStatement);
			WriteKeyword(UsingStatement.UsingKeywordRole);
			Space(policy.SpaceBeforeUsingParentheses);
			LPar();
			Space(policy.SpacesWithinUsingParentheses);
			
			DebugStart(usingStatement);
			usingStatement.ResourceAcquisition.AcceptVisitor(this);
			DebugEnd(usingStatement);
			
			Space(policy.SpacesWithinUsingParentheses);
			RPar();
			
			WriteEmbeddedStatement(usingStatement.EmbeddedStatement);
			
			EndNode(usingStatement);
		}
		
		public void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
		{
			StartNode(variableDeclarationStatement);
			DebugStart(variableDeclarationStatement);
			WriteModifiers(variableDeclarationStatement.GetChildrenByRole(VariableDeclarationStatement.ModifierRole));
			variableDeclarationStatement.Type.AcceptVisitor(this);
			Space();
			WriteCommaSeparatedList(variableDeclarationStatement.Variables);
			SemicolonDebugEnd(variableDeclarationStatement);
			EndNode(variableDeclarationStatement);
		}
		
		public void VisitWhileStatement(WhileStatement whileStatement)
		{
			StartNode(whileStatement);
			DebugStart(whileStatement);
			WriteKeyword(WhileStatement.WhileKeywordRole);
			Space(policy.SpaceBeforeWhileParentheses);
			LPar();
			Space(policy.SpacesWithinWhileParentheses);
			whileStatement.Condition.AcceptVisitor(this);
			Space(policy.SpacesWithinWhileParentheses);
			RPar();
			DebugEnd(whileStatement);
			WriteEmbeddedStatement(whileStatement.EmbeddedStatement);
			EndNode(whileStatement);
		}
		
		public void VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
		{
			StartNode(yieldBreakStatement);
			DebugStart(yieldBreakStatement);
			WriteKeyword(YieldBreakStatement.YieldKeywordRole);
			WriteKeyword(YieldBreakStatement.BreakKeywordRole);
			SemicolonDebugEnd(yieldBreakStatement);
			EndNode(yieldBreakStatement);
		}
		
		public void VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement)
		{
			StartNode(yieldReturnStatement);
			DebugStart(yieldReturnStatement);
			WriteKeyword(YieldReturnStatement.YieldKeywordRole);
			WriteKeyword(YieldReturnStatement.ReturnKeywordRole);
			Space();
			yieldReturnStatement.Expression.AcceptVisitor(this);
			SemicolonDebugEnd(yieldReturnStatement);
			EndNode(yieldReturnStatement);
		}

		#endregion
		
		#region TypeMembers
		public void VisitAccessor(Accessor accessor)
		{
			StartNode(accessor);
			WriteAttributes(accessor.Attributes);
			WriteModifiers(accessor.ModifierTokens);
			bool isDefault = accessor.Body.IsNull;
			if (isDefault)
				DebugStart(accessor);

			// Writer doesn't write the comment before accessor if nothing has been printed yet.
			// The following code works with our added comments.
			if (accessor.Attributes.Count == 0 && !accessor.ModifierTokens.Any()) {
				foreach (var child in accessor.Children) {
					var cmt = child as Comment;
					if (cmt == null)
						break;
					cmt.AcceptVisitor(this);
				}
			}

			if (accessor.Role == PropertyDeclaration.GetterRole) {
				WriteKeywordIdentifier(PropertyDeclaration.GetKeywordRole);
			} else if (accessor.Role == PropertyDeclaration.SetterRole) {
				WriteKeywordIdentifier(PropertyDeclaration.SetKeywordRole);
			} else if (accessor.Role == CustomEventDeclaration.AddAccessorRole) {
				WriteKeywordIdentifier(CustomEventDeclaration.AddKeywordRole);
			} else if (accessor.Role == CustomEventDeclaration.RemoveAccessorRole) {
				WriteKeywordIdentifier(CustomEventDeclaration.RemoveKeywordRole);
			}
			if (isDefault)
				SemicolonDebugEnd(accessor);
			else
				WriteMethodBody(accessor.Body);
			EndNode(accessor);
		}
		
		public void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
		{
			StartNode(constructorDeclaration);
			WriteAttributes(constructorDeclaration.Attributes);
			WriteModifiers(constructorDeclaration.ModifierTokens);
			TypeDeclaration type = constructorDeclaration.Parent as TypeDeclaration;
			var method = constructorDeclaration.Annotation<dnlib.DotNet.MethodDef>();
			var textToken = method == null ? TextTokenType.Type : TextTokenHelper.GetTextTokenType(method.DeclaringType);
			if (type != null && type.Name != constructorDeclaration.Name)
				WriteIdentifier((Identifier)type.NameToken.Clone(), textToken);
			else
				WriteIdentifier(constructorDeclaration.NameToken);
			Space(policy.SpaceBeforeConstructorDeclarationParentheses);
			WriteCommaSeparatedListInParenthesis(constructorDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
			if (!constructorDeclaration.Initializer.IsNull) {
				Space();
				constructorDeclaration.Initializer.AcceptVisitor(this);
			}
			WriteMethodBody(constructorDeclaration.Body);
			EndNode(constructorDeclaration);
		}
		
		public void VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
		{
			StartNode(constructorInitializer);
			WriteToken(Roles.Colon);
			Space();
			DebugStart(constructorInitializer);
			if (constructorInitializer.ConstructorInitializerType == ConstructorInitializerType.This) {
				WriteKeyword(ConstructorInitializer.ThisKeywordRole);
			} else {
				WriteKeyword(ConstructorInitializer.BaseKeywordRole);
			}
			Space(policy.SpaceBeforeMethodCallParentheses);
			WriteCommaSeparatedListInParenthesis(constructorInitializer.Arguments, policy.SpaceWithinMethodCallParentheses);
			DebugEnd(constructorInitializer);
			EndNode(constructorInitializer);
		}
		
		public void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
		{
			StartNode(destructorDeclaration);
			WriteAttributes(destructorDeclaration.Attributes);
			WriteModifiers(destructorDeclaration.ModifierTokens);

			// Writer doesn't write the comment before destructorDeclaration if nothing has been printed yet.
			// The following code works with our added comments.
			if (destructorDeclaration.Attributes.Count == 0 && !destructorDeclaration.ModifierTokens.Any()) {
				foreach (var child in destructorDeclaration.Children) {
					var cmt = child as Comment;
					if (cmt == null)
						break;
					cmt.AcceptVisitor(this);
				}
			}

			WriteToken(DestructorDeclaration.TildeRole);
			TypeDeclaration type = destructorDeclaration.Parent as TypeDeclaration;
			var method = destructorDeclaration.Annotation<dnlib.DotNet.MethodDef>();
			var textToken = method == null ? TextTokenType.Type : TextTokenHelper.GetTextTokenType(method.DeclaringType);
			if (type != null && type.Name != destructorDeclaration.Name)
				WriteIdentifier((Identifier)type.NameToken.Clone(), textToken);
			else
				WriteIdentifier(destructorDeclaration.NameToken, textToken);
			Space(policy.SpaceBeforeConstructorDeclarationParentheses);
			LPar();
			RPar();
			WriteMethodBody(destructorDeclaration.Body);
			EndNode(destructorDeclaration);
		}
		
		public void VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
		{
			StartNode(enumMemberDeclaration);
			WriteAttributes(enumMemberDeclaration.Attributes);
			WriteModifiers(enumMemberDeclaration.ModifierTokens);
			WriteIdentifier(enumMemberDeclaration.NameToken);
			if (!enumMemberDeclaration.Initializer.IsNull) {
				Space(policy.SpaceAroundAssignment);
				WriteToken(Roles.Assign);
				Space(policy.SpaceAroundAssignment);
				enumMemberDeclaration.Initializer.AcceptVisitor(this);
			}
			EndNode(enumMemberDeclaration);
		}
		
		public void VisitEventDeclaration(EventDeclaration eventDeclaration)
		{
			StartNode(eventDeclaration);
			WriteAttributes(eventDeclaration.Attributes);
			WriteModifiers(eventDeclaration.ModifierTokens);
			WriteKeyword(EventDeclaration.EventKeywordRole);
			eventDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WriteCommaSeparatedList(eventDeclaration.Variables);
			Semicolon();
			EndNode(eventDeclaration);
		}
		
		public void VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration)
		{
			StartNode(customEventDeclaration);
			WriteAttributes(customEventDeclaration.Attributes);
			WriteModifiers(customEventDeclaration.ModifierTokens);
			WriteKeyword(CustomEventDeclaration.EventKeywordRole);
			customEventDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WritePrivateImplementationType(customEventDeclaration.PrivateImplementationType);
			WriteIdentifier(customEventDeclaration.NameToken);
			OpenBrace(policy.EventBraceStyle);
			// output add/remove in their original order
			foreach (AstNode node in customEventDeclaration.Children) {
				if (node.Role == CustomEventDeclaration.AddAccessorRole || node.Role == CustomEventDeclaration.RemoveAccessorRole) {
					node.AcceptVisitor(this);
				}
			}
			CloseBrace(policy.EventBraceStyle);
			NewLine();
			EndNode(customEventDeclaration);
		}
		
		public void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
		{
			StartNode(fieldDeclaration);
			WriteAttributes(fieldDeclaration.Attributes);
			WriteModifiers(fieldDeclaration.ModifierTokens);
			writer.Space();
			DebugStart(fieldDeclaration);
			fieldDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WriteCommaSeparatedList(fieldDeclaration.Variables);
			SemicolonDebugEnd(fieldDeclaration);
			EndNode(fieldDeclaration);
		}
		
		public void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
		{
			StartNode(fixedFieldDeclaration);
			WriteAttributes(fixedFieldDeclaration.Attributes);
			WriteModifiers(fixedFieldDeclaration.ModifierTokens);
			WriteKeyword(FixedFieldDeclaration.FixedKeywordRole);
			Space();
			fixedFieldDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WriteCommaSeparatedList(fixedFieldDeclaration.Variables);
			Semicolon();
			EndNode(fixedFieldDeclaration);
		}
		
		public void VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer)
		{
			DebugExpression(fixedVariableInitializer);
			StartNode(fixedVariableInitializer);
			WriteIdentifier(fixedVariableInitializer.NameToken);
			if (!fixedVariableInitializer.CountExpression.IsNull) {
				WriteToken(Roles.LBracket);
				Space(policy.SpacesWithinBrackets);
				fixedVariableInitializer.CountExpression.AcceptVisitor(this);
				Space(policy.SpacesWithinBrackets);
				WriteToken(Roles.RBracket);
			}
			EndNode(fixedVariableInitializer);
		}
		
		public void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
		{
			StartNode(indexerDeclaration);
			WriteAttributes(indexerDeclaration.Attributes);
			WriteModifiers(indexerDeclaration.ModifierTokens);
			indexerDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WritePrivateImplementationType(indexerDeclaration.PrivateImplementationType);
			WriteKeyword(IndexerDeclaration.ThisKeywordRole);
			Space(policy.SpaceBeforeMethodDeclarationParentheses);
			WriteCommaSeparatedListInBrackets(indexerDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
			OpenBrace(policy.PropertyBraceStyle);
			// output get/set in their original order
			foreach (AstNode node in indexerDeclaration.Children) {
				if (node.Role == IndexerDeclaration.GetterRole || node.Role == IndexerDeclaration.SetterRole) {
					node.AcceptVisitor(this);
				}
			}
			CloseBrace(policy.PropertyBraceStyle);
			NewLine();
			EndNode(indexerDeclaration);
		}
		
		public void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
		{
			StartNode(methodDeclaration);
			WriteAttributes(methodDeclaration.Attributes);
			WriteModifiers(methodDeclaration.ModifierTokens);
			methodDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WritePrivateImplementationType(methodDeclaration.PrivateImplementationType);
			WriteIdentifier(methodDeclaration.NameToken);
			WriteTypeParameters(methodDeclaration.TypeParameters);
			Space(policy.SpaceBeforeMethodDeclarationParentheses);
			WriteCommaSeparatedListInParenthesis(methodDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
			foreach (Constraint constraint in methodDeclaration.Constraints) {
				constraint.AcceptVisitor(this);
			}
			WriteMethodBody(methodDeclaration.Body);
			EndNode(methodDeclaration);
		}
		
		public void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
		{
			StartNode(operatorDeclaration);
			WriteAttributes(operatorDeclaration.Attributes);
			WriteModifiers(operatorDeclaration.ModifierTokens);
			if (operatorDeclaration.OperatorType == OperatorType.Explicit) {
				WriteKeyword(OperatorDeclaration.ExplicitRole);
			} else if (operatorDeclaration.OperatorType == OperatorType.Implicit) {
				WriteKeyword(OperatorDeclaration.ImplicitRole);
			} else {
				operatorDeclaration.ReturnType.AcceptVisitor(this);
			}
			WriteKeywordIdentifier(OperatorDeclaration.OperatorKeywordRole);
			Space();
			if (operatorDeclaration.OperatorType == OperatorType.Explicit
			    || operatorDeclaration.OperatorType == OperatorType.Implicit) {
				operatorDeclaration.ReturnType.AcceptVisitor(this);
			} else {
				WriteToken(OperatorDeclaration.GetToken(operatorDeclaration.OperatorType), OperatorDeclaration.GetRole(operatorDeclaration.OperatorType));
			}
			Space(policy.SpaceBeforeMethodDeclarationParentheses);
			WriteCommaSeparatedListInParenthesis(operatorDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
			WriteMethodBody(operatorDeclaration.Body);
			EndNode(operatorDeclaration);
		}
		
		public void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
		{
			StartNode(parameterDeclaration);
			WriteAttributes(parameterDeclaration.Attributes);
			switch (parameterDeclaration.ParameterModifier) {
				case ParameterModifier.Ref:
					WriteKeyword(ParameterDeclaration.RefModifierRole);
					break;
				case ParameterModifier.Out:
					WriteKeyword(ParameterDeclaration.OutModifierRole);
					break;
				case ParameterModifier.Params:
					WriteKeyword(ParameterDeclaration.ParamsModifierRole);
					break;
				case ParameterModifier.This:
					WriteKeyword(ParameterDeclaration.ThisModifierRole);
					break;
			}
			parameterDeclaration.Type.AcceptVisitor(this);
			if (!parameterDeclaration.Type.IsNull && !string.IsNullOrEmpty(parameterDeclaration.Name)) {
				Space();
			}
			if (!string.IsNullOrEmpty(parameterDeclaration.Name)) {
				WriteIdentifier(parameterDeclaration.NameToken);
			}
			if (!parameterDeclaration.DefaultExpression.IsNull) {
				Space(policy.SpaceAroundAssignment);
				WriteToken(Roles.Assign);
				Space(policy.SpaceAroundAssignment);
				parameterDeclaration.DefaultExpression.AcceptVisitor(this);
			}
			EndNode(parameterDeclaration);
		}
		
		public void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			StartNode(propertyDeclaration);
			WriteAttributes(propertyDeclaration.Attributes);
			WriteModifiers(propertyDeclaration.ModifierTokens);
			propertyDeclaration.ReturnType.AcceptVisitor(this);
			Space();
			WritePrivateImplementationType(propertyDeclaration.PrivateImplementationType);
			WriteIdentifier(propertyDeclaration.NameToken);
			OpenBrace(policy.PropertyBraceStyle);
			// output get/set in their original order
			foreach (AstNode node in propertyDeclaration.Children) {
				if (node.Role == IndexerDeclaration.GetterRole || node.Role == IndexerDeclaration.SetterRole) {
					node.AcceptVisitor(this);
				}
			}
			CloseBrace(policy.PropertyBraceStyle);
			NewLine();
			EndNode(propertyDeclaration);
		}

		#endregion
		
		#region Other nodes
		public void VisitVariableInitializer(VariableInitializer variableInitializer)
		{
			StartNode(variableInitializer);
			WriteIdentifier(variableInitializer.NameToken);
			if (!variableInitializer.Initializer.IsNull) {
				Space(policy.SpaceAroundAssignment);
				WriteToken(Roles.Assign);
				Space(policy.SpaceAroundAssignment);
				variableInitializer.Initializer.AcceptVisitor(this);
			}
			EndNode(variableInitializer);
		}

		void MaybeNewLinesAfterUsings(AstNode node)
		{
			var nextSibling = node.NextSibling;
			while (nextSibling is WhitespaceNode || nextSibling is NewLineNode)
				nextSibling = nextSibling.NextSibling;

			if ((node is UsingDeclaration || node is UsingAliasDeclaration) && !(nextSibling is UsingDeclaration || nextSibling is UsingAliasDeclaration)) {
				for (int i = 0; i < policy.MinimumBlankLinesAfterUsings; i++)
					NewLine();
			}
		}
		
		public void VisitSyntaxTree(SyntaxTree syntaxTree)
		{
			// don't do node tracking as we visit all children directly
			foreach (AstNode node in syntaxTree.Children) {
				node.AcceptVisitor(this);
				MaybeNewLinesAfterUsings(node);
			}
		}
		
		public void VisitSimpleType(SimpleType simpleType)
		{
			StartNode(simpleType);
			if (simpleType.Identifier.Length == 0 && SimpleType.DummyTypeGenericParam.Equals(simpleType.Annotation<string>(), StringComparison.Ordinal)) {
				// It's the empty string. Don't call WriteIdentifier() since it will write "<<EMPTY_NAME>>"
			}
			else
				WriteIdentifier(simpleType.IdentifierToken, TextTokenHelper.GetTextTokenType(simpleType.IdentifierToken.Annotation<object>() ?? simpleType.Annotation<object>()));
			WriteTypeArguments(simpleType.TypeArguments);
			EndNode(simpleType);
		}
		
		public void VisitMemberType(MemberType memberType)
		{
			StartNode(memberType);
			memberType.Target.AcceptVisitor(this);
			if (memberType.IsDoubleColon) {
				WriteToken(Roles.DoubleColon);
			} else {
				WriteToken(Roles.Dot);
			}
			WriteIdentifier(memberType.MemberNameToken, TextTokenHelper.GetTextTokenType(memberType.MemberNameToken.Annotation<object>() ?? memberType.Annotation<object>()));
			WriteTypeArguments(memberType.TypeArguments);
			EndNode(memberType);
		}
		
		public void VisitComposedType(ComposedType composedType)
		{
			StartNode(composedType);
			composedType.BaseType.AcceptVisitor(this);
			if (composedType.HasNullableSpecifier) {
				WriteToken(ComposedType.NullableRole);
			}
			for (int i = 0; i < composedType.PointerRank; i++) {
				WriteToken(ComposedType.PointerRole);
			}
			foreach (var node in composedType.ArraySpecifiers) {
				node.AcceptVisitor(this);
			}
			EndNode(composedType);
		}
		
		public void VisitArraySpecifier(ArraySpecifier arraySpecifier)
		{
			StartNode(arraySpecifier);
			WriteToken(Roles.LBracket);
			foreach (var comma in arraySpecifier.GetChildrenByRole(Roles.Comma)) {
				writer.WriteTokenOperator(Roles.Comma, ",");
			}
			WriteToken(Roles.RBracket);
			EndNode(arraySpecifier);
		}
		
		public void VisitPrimitiveType(PrimitiveType primitiveType)
		{
			StartNode(primitiveType);
			writer.WritePrimitiveType(primitiveType.Keyword);
			EndNode(primitiveType);
		}
		
		public void VisitComment(Comment comment)
		{
			writer.StartNode(comment);
			writer.WriteComment(comment.CommentType, comment.Content, comment.References);
			writer.EndNode(comment);
		}

		public void VisitNewLine(NewLineNode newLineNode)
		{
//			formatter.StartNode(newLineNode);
//			formatter.NewLine();
//			formatter.EndNode(newLineNode);
		}

		public void VisitWhitespace(WhitespaceNode whitespaceNode)
		{
			// unused
		}

		public void VisitText(TextNode textNode)
		{
			// unused
		}

		public void VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective)
		{
			writer.StartNode(preProcessorDirective);
			writer.WritePreProcessorDirective(preProcessorDirective.Type, preProcessorDirective.Argument);
			writer.EndNode(preProcessorDirective);
		}
		
		public void VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration)
		{
			StartNode(typeParameterDeclaration);
			WriteAttributes(typeParameterDeclaration.Attributes);
			switch (typeParameterDeclaration.Variance) {
				case VarianceModifier.Invariant:
					break;
				case VarianceModifier.Covariant:
					WriteKeyword(TypeParameterDeclaration.OutVarianceKeywordRole);
					break;
				case VarianceModifier.Contravariant:
					WriteKeyword(TypeParameterDeclaration.InVarianceKeywordRole);
					break;
				default:
					throw new NotSupportedException ("Invalid value for VarianceModifier");
			}
			WriteIdentifier(typeParameterDeclaration.NameToken);
			EndNode(typeParameterDeclaration);
		}
		
		public void VisitConstraint(Constraint constraint)
		{
			StartNode(constraint);
			Space();
			WriteKeyword(Roles.WhereKeyword);
			constraint.TypeParameter.AcceptVisitor(this);
			Space();
			WriteToken(Roles.Colon);
			Space();
			WriteCommaSeparatedList(constraint.BaseTypes);
			EndNode(constraint);
		}
		
		public void VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode)
		{
			CSharpModifierToken mod = cSharpTokenNode as CSharpModifierToken;
			if (mod != null) {
				// ITokenWriter assumes that each node processed between a
				// StartNode(parentNode)-EndNode(parentNode)-pair is a child of parentNode.
				WriteKeyword(CSharpModifierToken.GetModifierName(mod.Modifier), cSharpTokenNode.Role);
			} else {
				throw new NotSupportedException ("Should never visit individual tokens");
			}
		}
		
		public void VisitIdentifier(Identifier identifier)
		{
			// Do not call StartNode and EndNode for Identifier, because they are handled by the ITokenWriter.
			// ITokenWriter assumes that each node processed between a
			// StartNode(parentNode)-EndNode(parentNode)-pair is a child of parentNode.
			WriteIdentifier(identifier, TextTokenHelper.GetTextTokenType(identifier.Annotation<object>()));
		}

		void IAstVisitor.VisitNullNode(AstNode nullNode)
		{
		}

		void IAstVisitor.VisitErrorNode(AstNode errorNode)
		{
			StartNode(errorNode);
			EndNode(errorNode);
		}
		#endregion

		#region Pattern Nodes
		public void VisitPatternPlaceholder(AstNode placeholder, PatternMatching.Pattern pattern)
		{
			StartNode(placeholder);
			VisitNodeInPattern(pattern);
			EndNode(placeholder);
		}
		
		void VisitAnyNode(AnyNode anyNode)
		{
			if (!string.IsNullOrEmpty(anyNode.GroupName)) {
				WriteIdentifier(anyNode.GroupName, TextTokenType.Text);
				WriteToken(Roles.Colon);
			}
		}
		
		void VisitBackreference(Backreference backreference)
		{
			WriteKeyword("backreference");
			LPar();
			WriteIdentifier(backreference.ReferencedGroupName, TextTokenType.Text);
			RPar();
		}
		
		void VisitIdentifierExpressionBackreference(IdentifierExpressionBackreference identifierExpressionBackreference)
		{
			WriteKeyword("identifierBackreference");
			LPar();
			WriteIdentifier(identifierExpressionBackreference.ReferencedGroupName, TextTokenType.Text);
			RPar();
		}
		
		void VisitChoice(Choice choice)
		{
			WriteKeyword("choice");
			Space();
			LPar();
			NewLine();
			writer.Indent();
			foreach (INode alternative in choice) {
				VisitNodeInPattern(alternative);
				if (alternative != choice.Last()) {
					WriteToken(Roles.Comma);
				}
				NewLine();
			}
			writer.Unindent();
			RPar();
		}
		
		void VisitNamedNode(NamedNode namedNode)
		{
			if (!string.IsNullOrEmpty(namedNode.GroupName)) {
				WriteIdentifier(namedNode.GroupName, TextTokenType.Text);
				WriteToken(Roles.Colon);
			}
			VisitNodeInPattern(namedNode.ChildNode);
		}
		
		void VisitRepeat(Repeat repeat)
		{
			WriteKeyword("repeat");
			LPar();
			if (repeat.MinCount != 0 || repeat.MaxCount != int.MaxValue) {
				WriteIdentifier(repeat.MinCount.ToString(), TextTokenType.Number);
				WriteToken(Roles.Comma);
				WriteIdentifier(repeat.MaxCount.ToString(), TextTokenType.Number);
				WriteToken(Roles.Comma);
			}
			VisitNodeInPattern(repeat.ChildNode);
			RPar();
		}
		
		void VisitOptionalNode(OptionalNode optionalNode)
		{
			WriteKeyword("optional");
			LPar();
			VisitNodeInPattern(optionalNode.ChildNode);
			RPar();
		}
		
		void VisitNodeInPattern(INode childNode)
		{
			if (childNode is AstNode) {
				((AstNode)childNode).AcceptVisitor(this);
			} else if (childNode is IdentifierExpressionBackreference) {
				VisitIdentifierExpressionBackreference((IdentifierExpressionBackreference)childNode);
			} else if (childNode is Choice) {
				VisitChoice((Choice)childNode);
			} else if (childNode is AnyNode) {
				VisitAnyNode((AnyNode)childNode);
			} else if (childNode is Backreference) {
				VisitBackreference((Backreference)childNode);
			} else if (childNode is NamedNode) {
				VisitNamedNode((NamedNode)childNode);
			} else if (childNode is OptionalNode) {
				VisitOptionalNode((OptionalNode)childNode);
			} else if (childNode is Repeat) {
				VisitRepeat((Repeat)childNode);
			} else {
				TextWriterTokenWriter.PrintPrimitiveValue(childNode);
			}
		}
		#endregion
		
		#region Documentation Reference
		public void VisitDocumentationReference(DocumentationReference documentationReference)
		{
			StartNode(documentationReference);
			if (!documentationReference.DeclaringType.IsNull) {
				documentationReference.DeclaringType.AcceptVisitor(this);
				if (documentationReference.SymbolKind != SymbolKind.TypeDefinition) {
					WriteToken(Roles.Dot);
				}
			}
			switch (documentationReference.SymbolKind) {
				case SymbolKind.TypeDefinition:
					// we already printed the DeclaringType
					break;
				case SymbolKind.Indexer:
					WriteKeyword(IndexerDeclaration.ThisKeywordRole);
					break;
				case SymbolKind.Operator:
					var opType = documentationReference.OperatorType;
					if (opType == OperatorType.Explicit) {
						WriteKeyword(OperatorDeclaration.ExplicitRole);
					} else if (opType == OperatorType.Implicit) {
						WriteKeyword(OperatorDeclaration.ImplicitRole);
					}
					WriteKeyword(OperatorDeclaration.OperatorKeywordRole);
					Space();
					if (opType == OperatorType.Explicit || opType == OperatorType.Implicit) {
						documentationReference.ConversionOperatorReturnType.AcceptVisitor(this);
					} else {
						WriteToken(OperatorDeclaration.GetToken(opType), OperatorDeclaration.GetRole(opType));
					}
					break;
				default:
					WriteIdentifier(documentationReference.GetChildByRole(Roles.Identifier), TextTokenType.Text);
					break;
			}
			WriteTypeArguments(documentationReference.TypeArguments);
			if (documentationReference.HasParameterList) {
				Space(policy.SpaceBeforeMethodDeclarationParentheses);
				if (documentationReference.SymbolKind == SymbolKind.Indexer) {
					WriteCommaSeparatedListInBrackets(documentationReference.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
				} else {
					WriteCommaSeparatedListInParenthesis(documentationReference.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
				}
			}
			EndNode(documentationReference);
		}
		#endregion
		
		/// <summary>
		/// Converts special characters to escape sequences within the given string.
		/// </summary>
		public static string ConvertString(string text)
		{
			return TextWriterTokenWriter.ConvertString(text);
		}
	}
}
