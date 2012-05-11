// 
// TemplateEngineProcessTemplateTests.cs
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
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating.Tests
{
	[TestFixture]
	public class TemplateEngineProcessTemplateTests
	{	
		[Test]
		public void Process_OutputDirectiveForFileExtension_SetFileExtensionCalledOnHost ()
		{
			string input = 
				"<#@ template language=\"C#\" #>\r\n" +
				"<#@ Output Extension=\".outputDirectiveExtension\" #>\r\n" +
				"Test\r\n";
			
			DummyHost host = new DummyHost ();
			Process (input, host);
			
			Assert.AreEqual (".outputDirectiveExtension", host.FileExtension);
		}
		
		[Test]
		public void Process_NoOutputDirectiveForFileExtension_SetFileExtensionNotCalledOnHost ()
		{
			string input = 
				"<#@ template language=\"C#\" #>\r\n" +
				"Test\r\n";
			
			DummyHost host = new DummyHost ();
			host.FileExtension = ".test";
			Process (input, host);
			
			Assert.AreEqual (".test", host.FileExtension);
		}
		
		[Test]
		public void Process_OutputDirectiveForFileExtensionMissingDot_SetFileExtensionCalledOnHostWithDotBeforeExtension ()
		{
			string input = 
				"<#@ template language=\"C#\" #>\r\n" +
				"<#@ Output Extension=\"outputDirectiveExtension\" #>\r\n" +
				"Test\r\n";
			
			DummyHost host = new DummyHost ();
			Process (input, host);
			
			Assert.AreEqual (".outputDirectiveExtension", host.FileExtension);
		}
		
		#region Helpers
		
		string Process (string input)
		{
			DummyHost host = new DummyHost ();
			return Process (input, host);
		}
		
		string Process (string input, DummyHost host)
		{
			TemplatingEngine engine = new TemplatingEngine ();
			string output = engine.ProcessTemplate (input, host);
			if (output != null) {
				output = output.Replace ("\r\n", "\n");
				return TemplatingEngineHelper.StripHeader (output, "\n");
			}
			return null;
		}
		
		#endregion
	}
}

