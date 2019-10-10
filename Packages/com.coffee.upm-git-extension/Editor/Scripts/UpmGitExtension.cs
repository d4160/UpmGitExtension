using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif


namespace Coffee.PackageManager
{
	[InitializeOnLoad]
	internal class UpmGitExtension : VisualElement, IPackageManagerExtension
	{
		//################################
		// Constant or Static Members.
		//################################
		static UpmGitExtension ()
		{
			//PackageManagerExtensions.RegisterExtension (new UpmGitExtension ());
		}

		//################################
		// Public Members.
		//################################
		/// <summary>
		/// Creates the extension UI visual element.
		/// </summary>
		/// <returns>A visual element that represents the UI or null if none</returns>
		public VisualElement CreateExtensionUI ()
		{
			phase = Phase.Initialize;
			return this;
		}

		/// <summary>
		/// Called by the Package Manager UI when a package is added or updated.
		/// </summary>
		/// <param name="packageInfo">The package information</param>
		public void OnPackageAddedOrUpdated (PackageInfo packageInfo)
		{
		}

		/// <summary>
		/// Called by the Package Manager UI when a package is removed.
		/// </summary>
		/// <param name="packageInfo">The package information</param>
		public void OnPackageRemoved (PackageInfo packageInfo)
		{
		}

		/// <summary>
		/// Called by the Package Manager UI when the package selection changed.
		/// </summary>
		/// <param name="packageInfo">The newly selected package information (can be null)</param>
		public void OnPackageSelectionChange (PackageInfo packageInfo)
		{
			InitializeUI ();
			if (phase == Phase.Initialize || packageInfo == null)
				return;

			Debug.Log("UpmGitExtension.OnPackageSelectionChange 1");
			// Update document actions.
			documentActions.SetPackageInfo(packageInfo);

			Debug.Log("UpmGitExtension.OnPackageSelectionChange 2");
			if (packageInfo.source == PackageSource.Git)
			{
			Debug.Log("UpmGitExtension.OnPackageSelectionChange 3");
				// Show remove button for git package.
				var removeButton = root.Q<Button>("remove");
			Debug.Log("UpmGitExtension.OnPackageSelectionChange 3.1 " + removeButton);
				UIUtils.SetElementDisplay(removeButton, true);
				removeButton.SetEnabled(true);

			Debug.Log("UpmGitExtension.OnPackageSelectionChange 4");
				// Show git tag.
				var tagGit = root.Q("tag-git");
				UIUtils.SetElementDisplay(tagGit, true);
			}
		}

		//################################
		// Private Static Members.
		//################################
		enum Phase
		{
			Initialize,
			Idle,
			UpdatePackages,
			ReloadPackageCollection,
		}
		
		static readonly string assemblyName = ", " + typeof(IPackageManagerExtension).Assembly.GetName().Name;
		static readonly string nameSpace = typeof(IPackageManagerExtension).Namespace + ".";
		static readonly Type tPackageWindow = Type.GetType(nameSpace + "PackageManagerWindow" + assemblyName);
		static readonly Type tPackageCollection = Type.GetType(nameSpace + "PackageCollection" + assemblyName);
		static readonly Type tUpmBaseOperation = Type.GetType(nameSpace + "UpmBaseOperation" + assemblyName);
		static readonly Regex regVersionValid = new Regex(@"^\d+", RegexOptions.Compiled);
		static readonly Regex regInstallVersion = new Regex(@"#.*$", RegexOptions.Compiled);
		static Expose _exPackageWindow;
		static EditorWindow _packageWindow;

		static Expose GetExposedPackages()
		{
#if UNITY_2019_3_OR_NEWER
			// return PackageDatabase.instance.m_Packages;
			var tPackageDb = Expose.GetType("UnityEditor.PackageManager.UI.PackageDatabase, UnityEditor");
			return Expose.FromType(tPackageDb).Get("instance").Get("m_Packages");
#elif UNITY_2019_1_OR_NEWER
			return Expose.FromType(tPackageCollection)["m_PackageList"];
#else
			if (_exPackageWindow == null || !_exPackageWindow.As<EditorWindow>())
				_exPackageWindow = GetExposedPackageWindow();
			return _exPackageWindow["Collection"] ["packages"];
#endif
		}

		static Expose GetExposedPackageWindow()
		{
			if(!_packageWindow)
				_packageWindow = Resources.FindObjectsOfTypeAll(tPackageWindow).FirstOrDefault() as EditorWindow;
			return Expose.FromObject(_packageWindow);
		}
		

		//################################
		// Private Members.
		//################################
		Phase phase;
		VisualElement root;
		DocumentActions documentActions;
		readonly Queue<Expose> gitPackages = new Queue<Expose>();

		/// <summary>
		/// Initializes UI.
		/// </summary>
		void InitializeUI ()
		{
			if (phase != Phase.Initialize)
				return;

			if (!DocumentActions.IsResourceReady() || !InstallPackageWindow.IsResourceReady() || !GitButton.IsResourceReady())
				return;

			test();

			root = UIUtils.GetRoot(this).Q<TemplateContainer>();

			Debug.Log("UpmGitExtension.InitializeUI 1");
			// Document actions.
			documentActions = new DocumentActions(root.Q("detailLinks"));

			Debug.Log("UpmGitExtension.InitializeUI 2");
			// Install package window.
			var installPackageWindow = new InstallPackageWindow();
			root.Add(installPackageWindow);

			Debug.Log("UpmGitExtension.InitializeUI 3");
			// Add button to open InstallPackageWindow
			var addButton = root.Q("toolbarAddButton") ?? root.Q("moreAddOptionsButton") ?? root.Q("toolbarAddMenu");
			var gitButton = new GitButton(installPackageWindow.Open);
			addButton.parent.Insert(addButton.parent.IndexOf(addButton) + 1, gitButton);

			Debug.Log("UpmGitExtension.InitializeUI 4");
#if UNITY_2018
			var space = new VisualElement();
			space.style.flexGrow = 1;
			addButton.parent.Insert(addButton.parent.IndexOf(addButton), space);
#endif

			Debug.Log("UpmGitExtension.InitializeUI 5");
			// Update git packages on load packages
			var packageList = Expose.FromObject(root.Q("packageList"));
			Debug.Log("UpmGitExtension.InitializeUI 5.1" + packageList.Value);

			Action onLoad = packageList["onPackageListLoaded"].As<Action>();
			Debug.Log("UpmGitExtension.InitializeUI 5.2" + onLoad);
			onLoad += OnPackageListLoaded;
			packageList["onPackageListLoaded"] = Expose.FromObject(onLoad);

			Debug.Log("UpmGitExtension.InitializeUI 6");
#if UNITY_2019_1_OR_NEWER
			var updateButton = root.Q("packageToolbar").Q<Button>("update");
#else
			OnPackageListLoaded();
			var updateButton = root.Q("updateCombo").Q<Button>("update");
#endif

			Debug.Log("UpmGitExtension.InitializeUI 7");
			var detailView = Expose.FromObject(root.Q("detailsGroup"));

			// Override click action.
			Action actionUpdate = () =>
			{
			
#if UNITY_2019_1_OR_NEWER
				var exTargetPackage = detailView["TargetVersion"];
#else
				var exTargetPackage = detailView["SelectedPackage"];
#endif
				if (exTargetPackage["Origin"].As<int>() == 99)
				{
					var packageId = exTargetPackage["Info"]["m_PackageId"].As<string>();
					string packageIdPrefix = regInstallVersion.Replace(packageId, "");
					string refName = exTargetPackage["Version"].ToString().Replace("0.0.0-", "");
					exTargetPackage["_PackageId"] = Expose.FromObject(packageIdPrefix + "#" + refName);
				}

				detailView.Call("UpdateClick");
			};

			Debug.Log("UpmGitExtension.InitializeUI 8");
			Expose.FromObject(updateButton.clickable)["clicked"] = Expose.FromObject(actionUpdate);

			phase = Phase.Idle;
		}

		
		void UpdateGitPackages(Queue<Expose> packagesToUpdate, Dictionary<string, IEnumerable<string>> results = null)
		{
			Debug.Log("UpdateGitPackages 1");

			Debug.LogFormat ("[UpdateGitPackages] {0} package(s) left", packagesToUpdate.Count);
			phase = Phase.UpdatePackages;
			bool isRunning = 0 < packagesToUpdate.Count;
			PlaySpinner(isRunning);

			Debug.Log("UpdateGitPackages 2");
			// Update task is finished.
			if (!isRunning)
			{
				Debug.LogFormat ("[UpdateGitPackages] Completed");
				// Nothing to do.
				if (results == null)
				{
					phase = Phase.Idle;
					return;
				}
					
				Debug.Log("UpdateGitPackages 3");
				// Update package infomation's version.
				Expose exPackages = GetExposedPackages();
				foreach (var pair in results)
				{
					try
					{
						Debug.Log("UpdateGitPackages 4");
						Debug.LogFormat ("[UpdateGitPackages] Overwrite {0}", pair.Key);
						if (exPackages.Call("ContainsKey", pair.Key).As<bool>())
							UpdatePackageInfoVersions(exPackages[pair.Key], pair.Value);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}

					Debug.Log("UpdateGitPackages 5");
				// Reload package collection on next frame
				Debug.LogFormat ("[UpdateGitPackages] Reload on next frame");
				phase = Phase.ReloadPackageCollection;
				EditorApplication.delayCall += ReloadPackageCollection;
				return;
			}
			
			if (results == null)
			{
				results = new Dictionary<string, IEnumerable<string>>();
			}

			Debug.Log("UpdateGitPackages 6");
			// Debug.Log("OnPackageListLoaded 1.2 " + p["recommendedVersion"]["packageInfo"]["source"].Value);
			//
			// 
			var package = packagesToUpdate.Dequeue();
			var displayPackage = package["recommendedVersion"]["packageInfo"];
			var packageId = displayPackage["packageId"].As<string>();
			var packageName = package["name"].As<string>();
			
			Debug.Log("UpdateGitPackages 7 " + results.ContainsKey(packageName));
			// Already get versions.
			if(packageId == null || results.ContainsKey(packageName))
			{
				Debug.LogFormat ("[UpdateGitPackages] Skip: {0}", packageName);
				UpdateGitPackages(packagesToUpdate, results);
				return;
			}
			
					Debug.Log("UpdateGitPackages 8");
			// Get all branch/tag names in repo.
			var url = PackageUtils.GetRepoUrlForCommand(packageId);
			Debug.LogFormat ("[UpdateGitPackages] GetRefs: {0}", packageName);
			GitUtils.GetRefs(url, refNames =>
			{
				results[packageName] = refNames;
				UpdateGitPackages(packagesToUpdate, results);
			});
		}

		void UpdatePackageInfoVersions(Expose exPackage, IEnumerable<string> versions)
		{
			Debug.Log("UpdatePackageInfoVersions 1");
			Expose vers = Expose.FromObject(versions
				.Select(x => regVersionValid.IsMatch(x) ? x : "0.0.0-" + x)
				.Distinct()
				.OrderBy(x => x)
				.ToArray());

			Debug.Log("UpdatePackageInfoVersions 2");
			var exInfo = exPackage["recommendedVersion"]["packageInfo"];
			var exVersions = exPackage["m_VersionList"]["m_Versions"];
			// exVersions["m_All"] = vers;
			// exVersions["m_Compatible"] = vers;

					Debug.Log("UpdatePackageInfoVersions 3");
			try
			{
				Type tUpmPackageVersion = Expose.GetType("UnityEditor.PackageManager.UI.UpmPackageVersion, UnityEditor");
				Type tSemVersion = Expose.GetType("UnityEditor.PackageManager.UI.SemVersion, UnityEditor");
				
				var semver = Expose.FromType(tSemVersion).Call("Parse","5.0.0", false);
				Debug.Log("UpdatePackageInfoVersions 3 " + semver);
				var upmPackageVersion = Expose.FromType(tUpmPackageVersion).New(exInfo.Value, false, semver.Value, exInfo["displayName"].Value);
				Debug.Log("UpdatePackageInfoVersions 3 " + upmPackageVersion);

				exVersions.Call("Add", upmPackageVersion);

				Expose exPackageInfoList = Expose.FromType(tUpmBaseOperation).Call("FromUpmPackageInfo", exInfo, true); // Convert to PackageInfos
				foreach (Expose x in exPackageInfoList)
				{
					x["Origin"] = Expose.FromObject(99);
#if UNITY_2019_2_OR_NEWER
					x["IsDiscoverable"] = Expose.FromObject(true);
#endif
				}
					Debug.Log("UpdatePackageInfoVersions 5");
				exPackage["source"] = exPackageInfoList;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		void PlaySpinner(bool playing)
		{
			var statusBar = root.Q("packageStatusBar");
			if (statusBar == null)
				return;

			var spinner = statusBar.Q("packageSpinner");
			if (spinner == null)
				return;

			Expose.FromObject(spinner).Call(playing ? "Start" : "Stop");
		}

		void ReloadPackageCollection()
		{
					Debug.Log("ReloadPackageCollection 0");
			var exPackageWindow = GetExposedPackageWindow();
			if (exPackageWindow == null)
				return;

					Debug.Log("ReloadPackageCollection 1");
			Debug.LogFormat ("[UpdateGitPackages] Reloading package collection...");
			exPackageWindow["Collection"].Call("UpdatePackageCollection", false);
		}

		//bool updateRequest = false;
		[MenuItem("hogehoge/hoge")]
		static void test()
		{
			var tPackageDb = Expose.GetType("UnityEditor.PackageManager.UI.PackageDatabase, UnityEditor");
			var packageDb = Expose.FromType(tPackageDb).Get("instance");
			Debug.Log(packageDb);

			var action = packageDb.Get("onRefreshOperationStart").As<Action>();
			Debug.Log(action);

			action += () =>{
				Debug.Log("onRefreshOperationStart");
			};
		}

		void OnPackageListLoaded()
		{
			Debug.Log("OnPackageListLoaded 0");
			Debug.LogFormat ("[UpdateGitPackages] OnPackageListLoaded {0}, {1}", phase, Time.frameCount);
			if (phase == Phase.ReloadPackageCollection)
			{
				phase = Phase.Idle;
				return;
			}
				
			try
			{
				Debug.Log("OnPackageListLoaded 1");
				Debug.Log("OnPackageListLoaded 1.0 " + GetExposedPackages());

				// Get repository refs as versions and update package infos.
				foreach (Expose p in GetExposedPackages()["Values"])
				{
					Debug.Log("OnPackageListLoaded 1.1 " + p["recommendedVersion"].Value);
					Debug.Log("OnPackageListLoaded 1.1 " + p["recommendedVersion"]["uniqueId"].Value);
					Debug.Log("OnPackageListLoaded 1.2 " + p["recommendedVersion"]["packageInfo"]["source"].Value);

					// Is it git package?
					var origin = p["recommendedVersion"]["packageInfo"]["source"].As<int>();
					if (origin == 5)
					{
						gitPackages.Enqueue(p);
					}
				}
				Debug.Log("OnPackageListLoaded 2");

				if (phase == Phase.Idle || phase == Phase.Initialize)
				{
					Debug.LogFormat ("[UpdateGitPackages] Start task to update git package.");

					Debug.Log("OnPackageListLoaded 3");

					UpdateGitPackages(gitPackages, null);
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}
}
