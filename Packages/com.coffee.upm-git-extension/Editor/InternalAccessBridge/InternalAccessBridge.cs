using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager.UI;
using UnityEngine.Experimental.UIElements;
using System.Text.RegularExpressions;

namespace UnityEditor.PackageManager.UI
{
	public class InternalBridge
	{

		private static InternalBridge instance = new InternalBridge ();
		public static InternalBridge Instance { get { return instance; } }


		LoadingSpinner loadingSpinner = null;
		PackageList packageList = null;
		PackageDetails packageDetails = null;

		private InternalBridge () { }

		public void Setup (VisualElement loadingSpinner, VisualElement packageList, VisualElement packageDetails)
		{
			this.loadingSpinner = loadingSpinner as LoadingSpinner;
			this.packageList = packageList as PackageList;
			this.packageDetails = packageDetails as PackageDetails;
		}


		static IEnumerable<Package> GetPackages ()
		{
			var collection = PackageCollection.Instance;
			return collection?.LatestListPackages
				.Select (x => x.Name)
				.Distinct ()
				.Select (collection.GetPackageByName)
				.Distinct () ?? Enumerable.Empty<Package> ();
		}

		public void AddCallback (Action action)
		{
			packageList.OnLoaded -= action;
			packageList.OnLoaded += action;
		}


		public void StartSpinner ()
		{
			loadingSpinner.Start ();
		}

		public void StopSpinner ()
		{
			loadingSpinner.Stop ();
		}


		public void SetElementDisplay (VisualElement element, bool visible)
		{
			UIUtils.SetElementDisplay (element, visible);
			element.visible = visible;
		}

		//public static string GetSelectedPackage ()
		//{
		//	var collection = PackageCollection.Instance;
		//	return collection?.SelectedPackage;
		//}

		//[MenuItem ("Package Test/ListSignalにコールバック仕込むテスト")]
		//public static void StopSpinner2 ()
		//{
		//	var collection = PackageCollection.Instance;
		//	collection.ListSignal.WhenOperation (testes);
		//}

		//static void testes (IListOperation op)
		//{
		//	Debug.Log ($"★★ WhenOperation {op.GetType ()} {op.OfflineMode}");



		//	var doneCb = Expose.FromObject (op)
		//			.Get ("_doneCallbackAction")
		//			.As<Action<IEnumerable<PackageInfo>>> ();

		//	//成功コールバックに追加
		//	doneCb += (IEnumerable<PackageInfo> packageInfos) =>
		//	{
		//		Debug.Log ("★★ _doneCallbackAction");
		//		Debug.Log (
		//		packageInfos
		//			.Select (x => $"{x.Name}: {x.Version} ({x.Info.versions.compatible.Aggregate ((a, b) => a + ',' + b)})\n")
		//			.Aggregate ((a, b) => a + ',' + b)
		//		);

		//		// このリストにはオフライン・オンライン全てのパッケージが含まれているはず
		//		//packageInfos
		//		//	.Where(p => p.Origin == PackageSource.Git) //Gitのみ

		//	};


		//	op.OnOperationFinalized += () =>
		//	{
		//		Debug.Log ("★★ OnOperationFinalized");
		//	};
		//}

		//[MenuItem ("Package Test/バージョン追加テスト1")]
		//public static void AssemblyTest2 ()
		//{
		//	var p = GetPackages ().First (x => x.Name.Contains ("github"));

		//	var packageInfoJson = MiniJSON.Json.Deserialize (JsonUtility.ToJson (p.VersionToDisplay.Info)) as Dictionary<string, object>;

		//	var versionsInfo = packageInfoJson ["m_Versions"] as Dictionary<string, object>;

		//	versionsInfo ["m_All"] = new string [] { "1.0.0", "2.0.0" };
		//	versionsInfo ["m_Compatible"] = new string [] { "1.0.0", "2.0.0" };

		//	p.VersionToDisplay.Info = JsonUtility.FromJson<UnityEditor.PackageManager.PackageInfo> (MiniJSON.Json.Serialize (packageInfoJson));

		//	Debug.Log (JsonUtility.ToJson (p.VersionToDisplay.Info, true));
		//	//.

		//	var source = Expose.FromType (typeof (UpmBaseOperation))
		//		.Call ("FromUpmPackageInfo", p.VersionToDisplay.Info, true)
		//		.As<IEnumerable<UnityEditor.PackageManager.UI.PackageInfo>> ();

		//	foreach (var version in source)
		//	{
		//		version.Origin = (PackageSource)99;
		//	}

		//	p.UpdateSource (source);
		//}


		//[MenuItem ("Package Test/バージョン追加テスト3(UI.PackageInfoのみで解決するパターン。これでも行けた)")]
		//public static void AssemblyTest3 ()
		//{
		//	var p = GetPackages ().First (x => x.Name.Contains ("github"));

		//	var pInfo = p.Current;

		//	var json = JsonUtility.ToJson (pInfo);

		//	var versions = new string [] { "1.0.0", "2.0.0" };


		//	List<PackageInfo> versionssss = new List<PackageInfo> ();
		//	versionssss.Add (pInfo);
		//	foreach (var v in versions)
		//	{
		//		var newPInfo = JsonUtility.FromJson (json, typeof (PackageInfo)) as PackageInfo;
		//		newPInfo.Version = Semver.SemVersion.Parse (v);
		//		newPInfo.IsCurrent = false;
		//		versionssss.Add (newPInfo);
		//	}

		//	p.UpdateSource (versionssss);
		//}

		static readonly Regex s_regRefs = new Regex ("refs/(tags|remotes/origin)/(.+),(.+)$", RegexOptions.Compiled);
		static readonly Regex s_RepoUrl = new Regex ("^([^@]+)@([^#]+)(#.+)?$", RegexOptions.Compiled);
		static readonly Regex s_VersionRef = new Regex (@"^([^\+]+)(\+.*)?$", RegexOptions.Compiled);

		//public static string GetRepoUrlForCommand (string packageId)
		//{
		//	Match m = Regex.Match (packageId, "^[^@]+@([^#]+)(#.+)?$");
		//	if (m.Success)
		//	{
		//		return m.Groups [1].Value;
		//	}
		//	return "";
		//}

		//	[MenuItem("hogehoge/hgoehoge")]
		//static void tettt()
		//{
		//	var m = s_VersionRef.Match ("0.5.0-preview+upm");
		//	Debug.Log (m.Success);
		//	Debug.Log (m.Groups[1]);
		//	Debug.Log (m.Groups[2]);
		//}

		public void InstallPackage(string packageName, string url, string version)
		{
			const string manifestPath = "Packages/manifest.json";
			var manifest = MiniJSON.Json.Deserialize (System.IO.File.ReadAllText (manifestPath)) as Dictionary<string, object>;
			var dependencies = manifest ["dependencies"] as Dictionary<string, object>;

			dependencies.Remove (packageName);
			dependencies.Add (packageName, url + "#" + version);

			System.IO.File.WriteAllText (manifestPath, MiniJSON.Json.Serialize (manifest));
			UnityEditor.AssetDatabase.Refresh (ImportAssetOptions.ForceUpdate);
		}



		public void UpdateCallback ()
		{
			Debug.Log ("UpdateCallback");
			var pInfo = this.packageDetails.VersionPopup.value.Version;

			Debug.Log (pInfo.Name);
			Debug.Log (pInfo.PackageId);
			Debug.Log (pInfo.Version);
			Debug.Log (pInfo.VersionId);
			Debug.Log (pInfo.Info.packageId);
			if (pInfo.Origin == (PackageSource)99)
			{
				var packageId = pInfo.Info.packageId;
				string url = s_RepoUrl.Replace (packageId, "$2");
				var m = s_VersionRef.Match (pInfo.Version.ToString ());
				var version = 0 < m.Groups [2].Length
					? m.Groups [2].Value.TrimStart('+')
					: m.Groups [1].Value;

				Debug.Log (pInfo.Version);
				Debug.Log (m.Groups [1].Value);
				Debug.Log (m.Groups [2].Value);

				InstallPackage (pInfo.Name, url, version);
			}
			else
			{
				Expose.FromObject (packageDetails).Call ("UpdateClick");
				//detailView.Call ("UpdateClick");
			}
		}

		static int frameCount = 0;

		[MenuItem ("Package Test/バージョン追加テスト4")]
		public static void AssemblyTest4 ()
		{
			if(frameCount == Time.frameCount)
			{
				return;
			}



			Debug.Log ($"AssemblyTest4");

			var gitPackages = GetPackages ()
				.Where (x => x.Current.Origin == PackageSource.Git || x.Current.Origin == (PackageSource)99)
				.ToArray();

			if (gitPackages.Length == 0) return;


			Instance.StartSpinner ();
			HashSet<string> names = new HashSet<string> (gitPackages.Select(p=>p.Current.Name));


			// 未アップデート
			foreach (var p in gitPackages)
			{
				var package = p;
				var pInfo = p.Current;
				pInfo.IsLatest = false;

				var packageName = pInfo.Name;
				pInfo.Origin = (PackageSource)99;
				var json = JsonUtility.ToJson (pInfo);
				var repoUrl = s_RepoUrl.Replace (pInfo.PackageId, "$2");
				Debug.Log ($"{pInfo.Name} -> {repoUrl}");


				GitUtils.GetRefs (pInfo.Name, repoUrl, refs =>
				{

					var versions = refs.Select (r => s_regRefs.Match (r))
					.Where (m => m.Success)
					.Select (m =>
					{
						var ver = m.Groups [2].Value == m.Groups [3].Value
						? m.Groups [2].Value
						: string.Format ("{0}+{1}", m.Groups [3].Value, m.Groups [2].Value);

						//Debug.Log ($"{ver}");

						var newPInfo = JsonUtility.FromJson (json, typeof (PackageInfo)) as PackageInfo;
						newPInfo.Version = Semver.SemVersion.Parse (m.Groups [2].Value);
						newPInfo.IsCurrent = pInfo.Version == newPInfo.Version;
						newPInfo.Origin = (PackageSource)99;
						newPInfo.Info = pInfo.Info;
						return newPInfo;
					});
					//.Concat (new [] { pInfo });


					Debug.Log ($"{pInfo.Name} -> {repoUrl} {versions.Count ()}");

					versions.OrderBy (v => v.Version).Last ().IsLatest = true;

					package.UpdateSource (versions);

					names.Remove (packageName);
					if (names.Count <= 0)
					{
						frameCount = Time.frameCount;
						Instance.StopSpinner ();

						Debug.LogFormat ("[UpdateGitPackages] Reloading package collection..." + Time.frameCount);
						var collection = PackageCollection.Instance;
						collection?.UpdatePackageCollection (false);
					}

				});


			}


			// var versions = new string [] { "1.0.0", "2.0.0" };


			// List<PackageInfo> versionssss = new List<PackageInfo> ();
			// versionssss.Add (pInfo);
			// foreach (var v in versions)
			// {
			// 	var newPInfo = JsonUtility.FromJson (json, typeof (PackageInfo)) as PackageInfo;
			// 	newPInfo.Version = Semver.SemVersion.Parse (v);
			// 	newPInfo.IsCurrent = false;
			// 	versionssss.Add (newPInfo);
			// }

			// p.UpdateSource (versionssss);
		}

		public void UpdatePackageCollection ()
		{
			var collection = PackageCollection.Instance;
			collection?.UpdatePackageCollection (false);
		}
	}
}


