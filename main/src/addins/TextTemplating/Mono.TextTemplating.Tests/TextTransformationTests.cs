// 
// TextTransformationTests.cs
//  
// Author:
//       Matt Ward
// 
// Copyright (c) 2012 Matt Ward
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
using System.CodeDom.Compiler;
using NUnit.Framework;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating.Tests
{
	public class TestableTextTransformation : TextTransformation
	{
		public override string TransformText ()
		{
			return "";
		}
		
		public CompilerError GetFirstError ()
		{
			return GetError (0);
		}
		
		public CompilerError GetError (int index)
		{
			return base.Errors[index];
		}
	}
	
	[TestFixture]
	public class TextTransformationTests
	{
		TestableTextTransformation textTransformation;
		
		[SetUp]
		public void Init ()
		{
			textTransformation = new TestableTextTransformation ();
		}
		
		[Test]
		public void Error_OneErrorMessgageAdded_CompilerErrorFileNameIsNotNull ()
		{
			textTransformation.Error ("Test");
			
			CompilerError compilerError = textTransformation.GetFirstError ();
			
			Assert.AreEqual ("", compilerError.FileName);
		}
		
		[Test]
		public void Warning_OneWarningMessgageAdded_CompilerErrorFileNameIsNotNull ()
		{
			textTransformation.Warning ("Test");
			
			CompilerError compilerError = textTransformation.GetFirstError ();
			
			Assert.AreEqual ("", compilerError.FileName);
		}
	}
}
