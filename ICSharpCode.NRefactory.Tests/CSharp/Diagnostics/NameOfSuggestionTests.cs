﻿//
// NameOfSuggestionTests.cs
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

using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class NameOfSuggestionTests : InspectionActionTestBase
	{
		[Test]
		public void TestArgumentNullException()
		{
			Analyze<NameOfSuggestionAnalyzer>(@"
using System;
class A
{
	void F(int foo)
	{
		throw new ArgumentNullException($""foo""$, ""bar"");
	}
}",@"
using System;
class A
{
	void F(int foo)
	{
		throw new ArgumentNullException(nameof(foo), ""bar"");
	}
}");
		}

		[Test]
		public void TestArgumentException()
		{
			Analyze<NameOfSuggestionAnalyzer>(@"
using System;
class A
{
	void F(object foo)
	{
		if(foo != null)
			throw new ArgumentException(""bar"", $""foo""$);
	}
}", @"
using System;
class A
{
	void F(object foo)
	{
		if(foo != null)
			throw new ArgumentException(""bar"", nameof(foo));
	}
}");
		}

		[Test]
		public void TestArgumentOutOfRangeExceptionSwap()
		{
			Analyze<NameOfSuggestionAnalyzer>(@"
using System;
class A
{
	void F(int foo)
	{
		throw new ArgumentOutOfRangeException($""foo""$, ""foo"");
	}
}", @"
using System;
class A
{
	void F(int foo)
	{
		throw new ArgumentOutOfRangeException(nameof(foo), ""foo"");
	}
}", 0);
		}

	}
}

