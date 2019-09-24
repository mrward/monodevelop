//
// UpdateMultipleNuGetPackagesActionTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
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
using System.Linq;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UpdateMultipleNuGetPackagesActionTests
	{
		TestableUpdateMultipleNuGetPackagesAction action;
		FakeSolutionManager solutionManager;
		FakeDotNetProject project;
		FakeNuGetProject nugetProject;
		List<SourceRepository> primaryRepositories;
		FakeNuGetPackageManager packageManager;
		FakePackageMetadataResource packageMetadataResource;
		PackageManagementEvents packageManagementEvents;
		FakeFileRemover fileRemover;
		FakePackageRestoreManager restoreManager;

		void CreateAction (
			string projectName = "MyProject",
			List<SourceRepository> secondarySources = null,
			params ProjectReference [] projectReferences)
		{
			project = new FakeDotNetProject (@"d:\projects\MyProject\MyProject.csproj");
			project.Name = projectName;
			project.References.AddRange (projectReferences);
			solutionManager = new FakeSolutionManager ();
			nugetProject = new FakeNuGetProject (project);
			solutionManager.NuGetProjects [project] = nugetProject;

			var metadataResourceProvider = new FakePackageMetadataResourceProvider ();
			packageMetadataResource = metadataResourceProvider.PackageMetadataResource;
			var source = new PackageSource ("http://test.com");
			var providers = new INuGetResourceProvider [] {
				metadataResourceProvider
			};
			var sourceRepository = new SourceRepository (source, providers);
			primaryRepositories = new [] {
				sourceRepository
			}.ToList ();

			solutionManager.SourceRepositoryProvider.Repositories.AddRange (secondarySources ?? primaryRepositories);

			action = new TestableUpdateMultipleNuGetPackagesAction (
				primaryRepositories,
				solutionManager);

			action.AddProject (project);

			packageManager = action.PackageManager;
			packageManagementEvents = action.PackageManagementEvents;
			fileRemover = action.FileRemover;
			restoreManager = action.RestoreManager;
		}

		void AddPackageToUpdate (string packageId, string version)
		{
			action.AddPackageToUpdate (new PackageIdentity (packageId, NuGetVersion.Parse (version)));
		}

		void AddInstallPackageIntoProjectAction (string packageId, string version)
		{
			var projectAction = new FakeNuGetProjectAction (packageId, version, NuGetProjectActionType.Install);
			packageManager.UpdateActions.Add (projectAction);
		}

		void AddUnrestoredPackageForProject (string projectName)
		{
			restoreManager.AddUnrestoredPackageForProject (projectName, solutionManager.SolutionDirectory);
		}

		void AddRestoredPackageForProject (string projectName)
		{
			restoreManager.AddRestoredPackageForProject (projectName, solutionManager.SolutionDirectory);
		}

		[Test]
		public void Execute_PackagesAreRestoredAndNoPrereleasePackages_ActionsResolvedFromNuGetPackageManager ()
		{
			CreateAction ();
			AddPackageToUpdate ("Test", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (primaryRepositories, packageManager.PreviewUpdatePrimarySources);
			Assert.AreEqual (primaryRepositories, packageManager.PreviewUpdateSecondarySources);
			Assert.AreEqual (nugetProject, packageManager.PreviewUpdateProjects.Single ());
			Assert.AreEqual ("Test", packageManager.PreviewUpdatePackages.Single ().Id);
			Assert.AreEqual ("1.0", packageManager.PreviewUpdatePackages.Single ().Version.ToString ());
			Assert.IsFalse (packageManager.PreviewUpdateResolutionContext.IncludePrerelease);
			Assert.AreEqual (VersionConstraints.None, packageManager.PreviewUpdateResolutionContext.VersionConstraints);
			Assert.IsTrue (packageManager.PreviewUpdateResolutionContext.IncludeUnlisted);
			Assert.AreEqual (DependencyBehavior.Lowest, packageManager.PreviewUpdateResolutionContext.DependencyBehavior);
		}

		[Test]
		public void Execute_SinglePrimarySource_TwoEnabledSources_SecondarySourcesAreEnabledSources ()
		{
			var secondarySources = new List<SourceRepository> ();
			secondarySources.Add (new SourceRepository (new PackageSource ("A"), Enumerable.Empty<INuGetResourceProvider> ()));
			secondarySources.Add (new SourceRepository (new PackageSource ("A"), Enumerable.Empty<INuGetResourceProvider> ()));
			CreateAction (secondarySources: secondarySources);
			AddPackageToUpdate ("Test", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (primaryRepositories, packageManager.PreviewUpdatePrimarySources);
			Assert.AreEqual (secondarySources, packageManager.PreviewUpdateSecondarySources);
		}

		[Test]
		public void Execute_PackagesAreRestoredAndNoPrereleasePackages_ActionsAvailableForInstrumentation ()
		{
			CreateAction ();
			AddPackageToUpdate ("Test", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			var provider = action as INuGetProjectActionsProvider;

			Assert.AreEqual (provider.GetNuGetProjectActions (), packageManager.UpdateActions);
		}

		[Test]
		public void Execute_PackagesAreRestoredAndNoPrereleasePackages_UpdatesPackageUsingResolvedActions ()
		{
			CreateAction ();
			AddPackageToUpdate ("Test", "1.0");
			AddPackageToUpdate ("A", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			AddInstallPackageIntoProjectAction ("A", "2.1");

			action.Execute ();

			Assert.AreEqual (packageManager.UpdateActions, packageManager.ExecutedActions);
			Assert.AreEqual (nugetProject, packageManager.ExecutedNuGetProjects.Single ());
			Assert.AreEqual (action.ProjectContext, packageManager.ExecutedProjectContext);
			Assert.AreEqual (packageManager.PreviewUpdateResolutionContext.SourceCacheContext, packageManager.ExecutedSourceCacheContext);
		}

		[Test]
		public void Execute_UpdatingToPrereleasePackage_ActionsResolvedFromNuGetPackageManagerWithIncludePrereleaseSetToTrue ()
		{
			CreateAction ();
			AddPackageToUpdate ("Test", "1.0.1-alpha");
			AddInstallPackageIntoProjectAction ("Test", "1.2.1-alpha");

			action.Execute ();

			Assert.IsTrue (packageManager.PreviewUpdateResolutionContext.IncludePrerelease);
		}

		[Test]
		public void Execute_NoActions_NoUpdateFoundEventFires ()
		{
			CreateAction ();
			AddPackageToUpdate ("Test", "1.0");
			IDotNetProject noUpdateFoundForProject = null;
			packageManagementEvents.NoUpdateFound += (sender, e) => {
				noUpdateFoundForProject = e.Project;
			};

			action.Execute ();

			Assert.AreEqual (project, noUpdateFoundForProject);
		}

		[Test]
		public void Execute_OnePackageAlreadyRestored_PackageIsNotRestored ()
		{
			CreateAction ("MyProject");
			AddPackageToUpdate ("Test", "1.0");
			solutionManager.SolutionDirectory = @"d:\projects\MyProject".ToNativePath ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			nugetProject.AddPackageReference ("Test", "1.2");
			AddRestoredPackageForProject ("MyProject");

			action.Execute ();

			Assert.IsNull (restoreManager.PackagesToBeRestored);
		}

		[Test]
		public void Execute_OnePackageNotRestored_PackageIsRestored ()
		{
			CreateAction ("MyProject");
			AddPackageToUpdate ("Test", "1.0");
			solutionManager.SolutionDirectory = @"d:\projects\MyProject".ToNativePath ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			nugetProject.AddPackageReference ("Test", "1.0");
			AddUnrestoredPackageForProject ("MyProject");

			action.Execute ();

			var packageRestoreData = restoreManager.PackagesToBeRestored.Single ();
			Assert.AreEqual (solutionManager.SolutionDirectory, restoreManager.RestoreMissingPackagesSolutionDirectory);
			Assert.AreEqual (action.ProjectContext, restoreManager.RestoreMissingPackagesProjectContext);
			Assert.AreEqual ("MyProject", packageRestoreData.ProjectNames.Single ());
			Assert.IsTrue (packageRestoreData.IsMissing);
			Assert.AreEqual ("Test", packageRestoreData.PackageReference.PackageIdentity.Id);
			Assert.AreEqual ("1.0", packageRestoreData.PackageReference.PackageIdentity.Version.ToString ());
		}

		[Test]
		public void Execute_OnePackageNotRestored_ProjectReferencesRefreshed ()
		{
			CreateAction ("MyProject");
			AddPackageToUpdate ("Test", "1.0");
			solutionManager.SolutionDirectory = @"d:\projects\MyProject".ToNativePath ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			AddUnrestoredPackageForProject ("MyProject");

			action.Execute ();

			Assert.IsTrue (project.IsReferenceStatusRefreshed);
		}

		[Test]
		public void Execute_OnePackageNotRestored_PackagesRestoredEventIsFired ()
		{
			CreateAction ("MyProject");
			AddPackageToUpdate ("Test", "1.0");
			solutionManager.SolutionDirectory = @"d:\projects\MyProject".ToNativePath ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			AddUnrestoredPackageForProject ("MyProject");
			bool packagesRestored = false;
			packageManagementEvents.PackagesRestored += (sender, e) => packagesRestored = true;

			action.Execute ();

			Assert.IsTrue (packagesRestored);
		}

		[Test]
		public void Execute_OnePackageNotRestoredAndPackageRestoreFails_ExceptionThrownAndRestoreFailureMessageLogged ()
		{
			CreateAction ("MyProject");
			AddPackageToUpdate ("Test", "1.0");
			solutionManager.SolutionDirectory = @"d:\projects\MyProject".ToNativePath ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			AddUnrestoredPackageForProject ("MyProject");
			string messageLogged = null;
			packageManagementEvents.PackageOperationMessageLogged += (sender, e) => {
				messageLogged = e.Message.ToString ();
			};
			restoreManager.BeforeRestoreMissingPackagesAsync = () => {
				var exception = new Exception ("RestoreErrorMessage");
				restoreManager.RaisePackageRestoreFailedEvent (exception, "MyProject");
			};

			var ex = Assert.Throws<AggregateException> (() => {
				action.Execute ();
			});

			Assert.AreEqual ("Package restore failed for project MyProject: RestoreErrorMessage", messageLogged);
			Assert.AreEqual ("Package restore failed.", ex.GetBaseException ().Message);
		}

		[Test]
		public void Execute_PackagesConfigFileDeletedDuringUpdate_FileServicePackagesConfigFileDeletionIsCancelled ()
		{
			CreateAction ();
			AddPackageToUpdate ("Test", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			string expectedFileName = @"d:\projects\MyProject\packages.config".ToNativePath ();
			bool? fileRemovedResult = null;
			packageManager.BeforeExecuteAction = () => {
				fileRemovedResult = packageManagementEvents.OnFileRemoving (expectedFileName);
			};

			action.Execute ();

			Assert.AreEqual (expectedFileName, fileRemover.FileRemoved);
			Assert.IsFalse (fileRemovedResult.Value);
		}

		[Test]
		public void Execute_ScriptFileDeletedDuringUpdate_FileDeletionIsNotCancelled ()
		{
			CreateAction ();
			AddPackageToUpdate ("Test", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			string fileName = @"d:\projects\MyProject\scripts\myscript.js".ToNativePath ();
			bool? fileRemovedResult = null;
			packageManager.BeforeExecuteAction = () => {
				fileRemovedResult = packageManagementEvents.OnFileRemoving (fileName);
			};

			action.Execute ();

			Assert.IsTrue (fileRemovedResult.Value);
			Assert.IsNull (fileRemover.FileRemoved);
		}

		[Test]
		public void Execute_ReferenceBeingUpdatedHasLocalCopyTrue_ReferenceAddedHasLocalCopyTrue ()
		{
			var originalProjectReference = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
			originalProjectReference.LocalCopy = true;
			CreateAction ("MyProject", null, originalProjectReference);
			AddPackageToUpdate ("Test", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var firstReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NewAssembly");
			var secondReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
			packageManager.BeforeExecuteActionTask = async () => {
				packageManagementEvents.OnReferenceRemoving (originalProjectReference);
				await nugetProject.ProjectReferenceMaintainer.RemoveReference (originalProjectReference);

				packageManagementEvents.OnReferenceAdding (firstReferenceBeingAdded);
				await nugetProject.ProjectReferenceMaintainer.AddReference (firstReferenceBeingAdded);

				packageManagementEvents.OnReferenceAdding (secondReferenceBeingAdded);
				await nugetProject.ProjectReferenceMaintainer.AddReference (secondReferenceBeingAdded);
			};

			action.Execute ();

			var nunitFrameworkReference = project.References.FirstOrDefault (r => r.Reference == originalProjectReference.Reference);
			var newReference = project.References.FirstOrDefault (r => r.Reference == "NewAssembly");
			Assert.IsTrue (newReference.LocalCopy);
			Assert.IsTrue (nunitFrameworkReference.LocalCopy);
		}

		[Test]
		public void Execute_ReferenceBeingUpdatedHasLocalCopyTrueButCaseIsDifferent_ReferenceAddedHasLocalCopyTrue ()
		{
			var originalProjectReference = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "nunit.framework");
			originalProjectReference.LocalCopy = true;
			CreateAction ("MyProject", null, originalProjectReference);
			AddPackageToUpdate ("Test", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var firstReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NewAssembly");
			var secondReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
			packageManager.BeforeExecuteActionTask = async () => {
				packageManagementEvents.OnReferenceRemoving (originalProjectReference);
				await nugetProject.ProjectReferenceMaintainer.RemoveReference (originalProjectReference);

				packageManagementEvents.OnReferenceAdding (firstReferenceBeingAdded);
				await nugetProject.ProjectReferenceMaintainer.AddReference (firstReferenceBeingAdded);

				packageManagementEvents.OnReferenceAdding (secondReferenceBeingAdded);
				await nugetProject.ProjectReferenceMaintainer.AddReference (secondReferenceBeingAdded);
			};

			action.Execute ();

			var nunitFrameworkReference = project.References.FirstOrDefault (r => r.Reference == originalProjectReference.Reference);
			var newReference = project.References.FirstOrDefault (r => r.Reference == "NewAssembly");
			Assert.IsTrue (newReference.LocalCopy);
			Assert.IsTrue (nunitFrameworkReference.LocalCopy);
		}

		[Test]
		public void Execute_ReferenceBeingUpdatedHasLocalCopyFalse_ReferenceAddedHasLocalCopyFalse ()
		{
			var originalProjectReference = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "nunit.framework");
			originalProjectReference.LocalCopy = false;
			CreateAction ("MyProject", null, originalProjectReference);
			AddPackageToUpdate ("Test", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var firstReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NewAssembly");
			firstReferenceBeingAdded.LocalCopy = true;
			var secondReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
			packageManager.BeforeExecuteActionTask = async () => {
				packageManagementEvents.OnReferenceRemoving (originalProjectReference);
				await nugetProject.ProjectReferenceMaintainer.RemoveReference (originalProjectReference);

				packageManagementEvents.OnReferenceAdding (firstReferenceBeingAdded);
				await nugetProject.ProjectReferenceMaintainer.AddReference (firstReferenceBeingAdded);

				packageManagementEvents.OnReferenceAdding (secondReferenceBeingAdded);
				await nugetProject.ProjectReferenceMaintainer.AddReference (secondReferenceBeingAdded);
			};
			action.Execute ();

			var nunitFrameworkReference = project.References.FirstOrDefault (r => r.Reference == originalProjectReference.Reference);
			var newReference = project.References.FirstOrDefault (r => r.Reference == "NewAssembly");
			Assert.IsTrue (newReference.LocalCopy);
			Assert.IsFalse (nunitFrameworkReference.LocalCopy);
		}

		[Test]
		public void Execute_ReferenceBeingUpdatedHasLocalCopyFalseButCaseIsDifferent_ReferenceAddedHasLocalCopyFalse ()
		{
			var originalProjectReference = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "nunit.framework");
			originalProjectReference.LocalCopy = false;
			CreateAction ("MyProject", null, originalProjectReference);
			AddPackageToUpdate ("Test", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var firstReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NewAssembly");
			firstReferenceBeingAdded.LocalCopy = true;
			var secondReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
			packageManager.BeforeExecuteActionTask = async () => {
				packageManagementEvents.OnReferenceRemoving (originalProjectReference);
				await nugetProject.ProjectReferenceMaintainer.RemoveReference (originalProjectReference);

				packageManagementEvents.OnReferenceAdding (firstReferenceBeingAdded);
				await nugetProject.ProjectReferenceMaintainer.AddReference (firstReferenceBeingAdded);

				packageManagementEvents.OnReferenceAdding (secondReferenceBeingAdded);
				await nugetProject.ProjectReferenceMaintainer.AddReference (secondReferenceBeingAdded);
			};

			action.Execute ();

			var nunitFrameworkReference = project.References.FirstOrDefault (r => r.Reference == originalProjectReference.Reference);
			var newReference = project.References.FirstOrDefault (r => r.Reference == "NewAssembly");
			Assert.IsTrue (newReference.LocalCopy);
			Assert.IsFalse (nunitFrameworkReference.LocalCopy);
		}

		[Test]
		public void Execute_PackagesConfigFileNamedAfterProjectDeletedDuringUpdate_FileServicePackagesConfigFileDeletionIsCancelled ()
		{
			CreateAction ();
			AddPackageToUpdate ("Test", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			string expectedFileName = @"d:\projects\MyProject\packages.MyProject.config".ToNativePath ();
			bool? fileRemovedResult = null;
			packageManager.BeforeExecuteAction = () => {
				fileRemovedResult = packageManagementEvents.OnFileRemoving (expectedFileName);
			};
			action.Execute ();

			Assert.AreEqual (expectedFileName, fileRemover.FileRemoved);
			Assert.IsFalse (fileRemovedResult.Value);
		}

		[Test]
		public void Execute_PackageHasALicenseToBeAcceptedWhichIsAccepted_UserPromptedToAcceptLicenses ()
		{
			CreateAction ();
			AddPackageToUpdate ("Test", "1.0");
			action.LicenseAcceptanceService.AcceptLicensesReturnValue = true;
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var metadata = packageMetadataResource.AddPackageMetadata ("Test", "1.2");
			metadata.RequireLicenseAcceptance = true;
			metadata.LicenseUrl = new Uri ("http://test.com/license");

			action.Execute ();

			var license = action.LicenseAcceptanceService.PackageLicensesAccepted.Single ();
			Assert.AreEqual ("Test", license.PackageId);
			Assert.AreEqual (metadata.LicenseUrl, license.LicenseUrl);
			Assert.AreEqual (metadata.Authors, license.PackageAuthor);
			Assert.AreEqual (metadata.Title, license.PackageTitle);
			Assert.AreEqual ("Test", license.PackageIdentity.Id);
			Assert.AreEqual ("1.2", license.PackageIdentity.Version.ToString ());
		}

		[Test]
		public void Execute_PackageHasALicenseToBeAcceptedWhichIsNotAccepted_ExceptionThrown ()
		{
			CreateAction ();
			AddPackageToUpdate ("Test", "1.0");
			action.LicenseAcceptanceService.AcceptLicensesReturnValue = false;
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var metadata = packageMetadataResource.AddPackageMetadata ("Test", "1.2");
			metadata.RequireLicenseAcceptance = true;
			metadata.LicenseUrl = new Uri ("http://test.com/license");

			Exception ex = Assert.Throws (typeof (AggregateException), () => action.Execute ());

			Assert.AreEqual ("Licenses not accepted.", ex.GetBaseException ().Message);
		}
	}
}
