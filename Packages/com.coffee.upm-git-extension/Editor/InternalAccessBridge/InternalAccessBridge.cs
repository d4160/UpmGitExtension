﻿using System;
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
		LoadingSpinner loadingSpinner = null;
		PackageList packageList = null;
		PackageDetails packageDetails = null;


		public InternalBridge (VisualElement loadingSpinner, VisualElement packageList, VisualElement packageDetails)
		{
			this.loadingSpinner = loadingSpinner as LoadingSpinner;
			this.packageList = packageList as PackageList;
			this.packageDetails = packageDetails as PackageDetails;
		}


		static IEnumerable<Package> GetPackages ()
		{
			var collection = PackageCollection.Instance;
			return collection?.LatestListPackages
				.Select (x => collection.GetPackageByName (x.Name))
				.Distinct () ?? Enumerable.Empty<Package> ();
		}

		public void AddCallback (Action action)
		{
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

		public static string GetSelectedPackage ()
		{
			var collection = PackageCollection.Instance;
			return collection?.SelectedPackage;
		}

		[MenuItem ("Package Test/ListSignalにコールバック仕込むテスト")]
		public static void StopSpinner2 ()
		{
			var collection = PackageCollection.Instance;
			collection.ListSignal.WhenOperation (testes);
		}

		static void testes (IListOperation op)
		{
			Debug.Log ($"★★ WhenOperation {op.GetType()} {op.OfflineMode}");



			var doneCb = Expose.FromObject (op)
					.Get ("_doneCallbackAction")
					.As<Action<IEnumerable<PackageInfo>>> ();

			//成功コールバックに追加
			doneCb += (IEnumerable<PackageInfo> packageInfos) =>
			{
				Debug.Log ("★★ _doneCallbackAction");
				Debug.Log (
				packageInfos
					.Select (x => $"{x.Name}: {x.Version} ({x.Info.versions.compatible.Aggregate ((a, b) => a + ',' + b)})\n")
					.Aggregate ((a, b) => a + ',' + b)
				);

				// このリストにはオフライン・オンライン全てのパッケージが含まれているはず
				//packageInfos
				//	.Where(p => p.Origin == PackageSource.Git) //Gitのみ

			};


			op.OnOperationFinalized += () =>
			{
				Debug.Log ("★★ OnOperationFinalized");
			};
		}

		[MenuItem ("Package Test/バージョン追加テスト1")]
		public static void AssemblyTest2 ()
		{
			var p = GetPackages ().First (x => x.Name.Contains ("github"));

			var packageInfoJson = MiniJSON.Json.Deserialize (JsonUtility.ToJson (p.VersionToDisplay.Info)) as Dictionary<string, object>;

			var versionsInfo = packageInfoJson ["m_Versions"] as Dictionary<string, object>;

			versionsInfo ["m_All"] = new string [] { "1.0.0", "2.0.0" };
			versionsInfo ["m_Compatible"] = new string [] { "1.0.0", "2.0.0" };

			p.VersionToDisplay.Info = JsonUtility.FromJson<UnityEditor.PackageManager.PackageInfo> (MiniJSON.Json.Serialize (packageInfoJson));

			Debug.Log (JsonUtility.ToJson (p.VersionToDisplay.Info, true));
			//.

			var source = Expose.FromType (typeof (UpmBaseOperation))
				.Call ("FromUpmPackageInfo", p.VersionToDisplay.Info, true)
				.As<IEnumerable<UnityEditor.PackageManager.UI.PackageInfo>> ();

			foreach (var version in source)
			{
				version.Origin = (PackageSource)99;
			}

			p.UpdateSource (source);
		}


		[MenuItem ("Package Test/バージョン追加テスト3(UI.PackageInfoのみで解決するパターン。これでも行けた)")]
		public static void AssemblyTest3 ()
		{
			var p = GetPackages ().First (x => x.Name.Contains ("github"));

			var pInfo = p.Current;

			var json = JsonUtility.ToJson (pInfo);

			var versions = new string [] { "1.0.0", "2.0.0" };


			List<PackageInfo> versionssss = new List<PackageInfo> ();
			versionssss.Add (pInfo);
			foreach (var v in versions)
			{
				var newPInfo = JsonUtility.FromJson (json, typeof (PackageInfo)) as PackageInfo;
				newPInfo.Version = Semver.SemVersion.Parse (v);
				newPInfo.IsCurrent = false;
				versionssss.Add (newPInfo);
			}

			p.UpdateSource (versionssss);
		}

        static readonly Regex s_regRefs = new Regex("refs/(tags|remotes/origin)/(.+),(.+)$", RegexOptions.Compiled);

		[MenuItem ("Package Test/バージョン追加テスト4")]
		public static void AssemblyTest4 ()
		{
			Debug.Log($"AssemblyTest4");

			// 未アップデート
			foreach(var p in GetPackages ().Where (x => x.Current.Origin == PackageSource.Git || x.Current.Origin == (PackageSource)99))
			{


				var pInfo = p.Current;
				pInfo.Origin = (PackageSource)99;
				Debug.Log($"{pInfo.Name}");

				var json = JsonUtility.ToJson (pInfo);
				var repoUrl = "git@github.com:mob-sakai/UpmGitExtension.git";
				GitUtils.GetRefs(repoUrl, refs =>{


					var versions = refs.Select(r=>s_regRefs.Match(r))
					.Where(m=>m.Success)
					.Select(m=>{
						var ver = m.Groups[2].Value == m.Groups[3].Value
						? m.Groups[2].Value
						: string.Format("{0}+{1}",m.Groups[3].Value,m.Groups[2].Value);

						Debug.Log($"{ver}");

						var newPInfo = JsonUtility.FromJson (json, typeof (PackageInfo)) as PackageInfo;
						newPInfo.Version = Semver.SemVersion.Parse (m.Groups[2].Value);
						newPInfo.IsCurrent = false;
						newPInfo.Origin = (PackageSource)99;
						return newPInfo;
					})
					.Concat(new []{pInfo});


					// Debug.Log($"{refs.Count()}");

					// foreach(var r in refs)
					// {
					// Debug.Log($"{r}");
					// }

					// var versions = refs.Select(r=>{
					// 	var newPInfo = JsonUtility.FromJson (json, typeof (PackageInfo)) as PackageInfo;
					// 	newPInfo.Version = Semver.SemVersion.Parse (r);
					// 	newPInfo.IsCurrent = false;
					// 	newPInfo.Origin = (PackageSource)99;
					// 	return newPInfo;
					// }).Concat(new []{pInfo});

					Debug.Log($"{versions.Count()}");

					p.UpdateSource (versions);
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


