//
// DotNetCoreSdkLocationWidget.UI.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

using Xwt;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;

namespace MonoDevelop.DotNetCore.Gui
{
	partial class DotNetCoreSdkLocationWidget : Widget
	{
		FileSelector locationFileSelector;
		Label messageLabel;
		ImageView messageIcon;

		void Build ()
		{
			var mainVBox = new VBox ();
			mainVBox.Spacing = 12;

			var titleLabel = new Label ();
			titleLabel.Markup = GetBoldMarkup (GettextCatalog.GetString (".NET Core SDK"));
			mainVBox.PackStart (titleLabel);

			var topVBox = new VBox ();
			topVBox.Spacing = 6;
			topVBox.MarginLeft = 24;
			mainVBox.PackStart (topVBox);

			var messageBox = new HBox ();
			messageBox.Spacing = 6;
			topVBox.PackStart (messageBox, false, false);

			messageIcon = new ImageView ();
			messageBox.PackStart (messageIcon, false, false);

			messageLabel = new Label ();
			messageBox.PackStart (messageLabel, true, true);

			var locationBox = new HBox ();
			locationBox.Spacing = 6;
			topVBox.PackStart (locationBox, false, false);

			var locationLabel = new Label ();
			locationLabel.Text = GettextCatalog.GetString (".NET Core Command Line:");
			locationBox.PackStart (locationLabel, false, false);

			locationFileSelector = new FileSelector ();
			locationBox.PackStart (locationFileSelector, true, true);

			Content = mainVBox;
		}

		static string GetBoldMarkup (string text)
		{
			return "<b>" + GLib.Markup.EscapeText (text) + "</b>";
		}

		void UpdateIconAccessibility (bool found)
		{
			messageIcon.SetCommonAccessibilityAttributes (
				"LocationImage",
				found ? GettextCatalog.GetString ("A Tick") : GettextCatalog.GetString ("A Cross"),
				found ? GettextCatalog.GetString ("The SDK was found") : GettextCatalog.GetString ("The SDK was not found"));
		}
	}
}
