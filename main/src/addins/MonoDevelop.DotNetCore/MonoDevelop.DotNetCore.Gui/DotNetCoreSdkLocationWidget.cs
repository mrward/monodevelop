//
// DotNetCoreSdkLocationWidget.cs
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

using System;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.DotNetCore.Gui
{
	partial class DotNetCoreSdkLocationWidget
	{
		DotNetCoreSdkLocationPanel panel;

		public DotNetCoreSdkLocationWidget (DotNetCoreSdkLocationPanel panel)
		{
			this.panel = panel;

			Build ();

			string location = panel.LoadSdkLocationSetting ();
			locationFileSelector.FileName = location ?? string.Empty;

			locationFileSelector.FileChanged += LocationChanged;

			Validate ();
		}

		void LocationChanged (object sender, EventArgs e)
		{
			Validate ();
		}

		void Validate ()
		{
			FilePath location = CleanPath (locationFileSelector.FileName);
			if (!location.IsNullOrEmpty) {
				if (panel.ValidateSdkLocation (location)) {
					messageLabel.Text = GettextCatalog.GetString ("SDK found at specified location.");
					messageIcon.Image = ImageService.GetIcon (Gtk.Stock.Apply, Gtk.IconSize.Menu);
					UpdateIconAccessibility (true);
					return;
				}
				messageLabel.Text = GettextCatalog.GetString ("No SDK found at specified location.");
				messageIcon.Image = ImageService.GetIcon (Gtk.Stock.Cancel, Gtk.IconSize.Menu);
				UpdateIconAccessibility (false);
				return;
			}

			foreach (string defaultLocation in panel.DefaultSdkLocations) {
				if (panel.ValidateSdkLocation (defaultLocation)) {
					messageLabel.Text = GettextCatalog.GetString ("SDK found at default location.");
					messageIcon.Image = ImageService.GetIcon (Gtk.Stock.Apply, Gtk.IconSize.Menu);
					UpdateIconAccessibility (true);
					return;
				}
			}

			messageLabel.Text = GettextCatalog.GetString ("No SDK found at default location.");
			messageIcon.Image = ImageService.GetIcon (Gtk.Stock.Cancel, Gtk.IconSize.Menu);
			UpdateIconAccessibility (false);
		}

		FilePath CleanPath (FilePath path)
		{
			if (path.IsNullOrEmpty) {
				return null;
			}

			try {
				return path.FullPath;
			} catch {
				return null;
			}
		}

		public void ApplyChanges ()
		{
			panel.SaveSdkLocationSetting (CleanPath (locationFileSelector.FileName));
		}
	}
}
