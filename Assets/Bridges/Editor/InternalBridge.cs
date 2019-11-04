using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager.UI;
using Coffee.PackageManager;

namespace UnityEditor.PackageManager.UI
{
	public class InternalBridge
	{
		LoadingSpinner loadingSpinner = null;
		PackageList packageList = null;

		public InternalBridge (object loadingSpinner, object packageList)
		{
			this.loadingSpinner = loadingSpinner as LoadingSpinner;
			this.packageList = packageList as PackageList;
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

		public static string GetSelectedPackage ()
		{
			var collection = PackageCollection.Instance;
			return collection?.SelectedPackage;
		}

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

			foreach(var version in source)
			{
				version.Origin = (PackageSource)99;
			}

			p.UpdateSource (source);
		}

		public void UpdatePackageCollection ()
		{
			var collection = PackageCollection.Instance;
			collection?.UpdatePackageCollection (false);
		}
	}
}


