﻿//
// NRefactoryDiagnosticDescriptor.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	public class NRefactoryDiagnosticDescriptor : DiagnosticDescriptor
	{
		/// <summary>
		/// Gets or sets the Issue marker which should be used to mark this issue in the editor.
		/// It's up to the editor implementation if and how this info is used.
		/// </summary>
		public IssueMarker IssueMarker {
			get;
			internal set;
		}
		
		public string AnalysisDisableKeyword { get; set; }
		public string SuppressMessageCategory { get; set; }
		public string SuppressMessageCheckId { get; set; }
		public int PragmaWarning { get; set; }
		public bool IsEnabledByDefault { get; set; }
		
		public NRefactoryDiagnosticDescriptor(string id, string kind, string name, string messageTemplate, string category, DiagnosticSeverity severity) : base(id, kind, name, messageTemplate, category, severity)
		{
			IssueMarker = IssueMarker.WavedLine;
		}
	}

	public class NRefactorySubDiagnosticDescriptor : DiagnosticDescriptor
	{
		public string ParentId {
			get;
			private set;
		}

		public bool? IsEnabledByDefault { get; set; }

		public NRefactorySubDiagnosticDescriptor(string parentId, string id, string kind, string name, string messageTemplate, string category, DiagnosticSeverity severity) : base(id, kind, name, messageTemplate, category, severity)
		{
			if (parentId == null)
				throw new ArgumentNullException("parentId");
			ParentId = parentId;
		}
	}
}

