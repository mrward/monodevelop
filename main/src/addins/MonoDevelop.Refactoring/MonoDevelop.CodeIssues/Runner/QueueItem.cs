//
// QueueItem.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Simon Lindgren
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
using MonoDevelop.Projects;
using System.Threading;
using System.Linq;

namespace MonoDevelop.CodeIssues
{
	/// <summary>
	/// Represents a unit of analysis at the time of analysis. Essentially this
	/// maps a file to the jobs that wants to run analysis on it so the runner
	/// can parse the file once and then make progress on all the jobs.
	/// This class is not thread safe.
	/// </summary>
	public class QueueItem : IComparable<QueueItem>
	{
		readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

		/// <summary>
		/// The jobs to run on the file specified in <see cref="FileName"/>.
		/// </summary>
		readonly IList<IAnalysisJob> jobs = new List<IAnalysisJob>();

		/// <summary>
		/// The name of a file to be analyzed.
		/// </summary>
		/// <value>The name of the file.</value>
		public ProjectFile File { get; private set; }

		/// <summary>
		/// Gets the cancellation token for this work unit.
		/// </summary>
		/// <value>A cancellation token.</value>
		public CancellationToken CancellationToken {
			get {
				return tokenSource.Token;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MonoDevelop.CodeIssues.QueueItem"/> class.
		/// </summary>
		/// <param name="file">The file.</param>
		public QueueItem (ProjectFile file)
		{
			File = file;
		}

		/// <summary>
		/// Adds a job to be run on this file.
		/// </summary>
		/// <param name="job">The job.</param>
		public void AddJob (IAnalysisJob job)
		{
			jobs.Add (job);
		}

		/// <summary>
		/// Gets the current jobs.
		/// </summary>
		/// <returns>The jobs.</returns>
		public IEnumerable<IAnalysisJob> GetJobs ()
		{
			return new List<IAnalysisJob> (jobs);
		}

		/// <summary>
		/// Removes the specified job.
		/// If the job is the last job in this instance it requests cancellation of it's CancellationToken.
		/// </summary>
		/// <param name="job">The job to remove.</param>
		public void RemoveJob(IAnalysisJob job)
		{
			jobs.Remove (job);
			if (!jobs.Any ())
				tokenSource.Cancel ();
		}

		#region IComparable implementation

		public int CompareTo (QueueItem other)
		{
			return jobs.Count.CompareTo (other.jobs.Count);
		}

		#endregion
	}
}

