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

        private static InternalBridge instance = new InternalBridge();
        public static InternalBridge Instance { get { return instance; } }


        LoadingSpinner loadingSpinner = null;
        PackageList packageList = null;
        PackageDetails packageDetails = null;

        private InternalBridge() { }

        public void Setup(VisualElement loadingSpinner, VisualElement packageList, VisualElement packageDetails)
        {
            this.loadingSpinner = loadingSpinner as LoadingSpinner;
            this.packageList = packageList as PackageList;
            this.packageDetails = packageDetails as PackageDetails;
        }


        static IEnumerable<Package> GetPackages()
        {
            var collection = PackageCollection.Instance;
            return collection?.LatestListPackages
                .Select(x => x.Name)
                .Distinct()
                .Select(collection.GetPackageByName)
                .Distinct() ?? Enumerable.Empty<Package>();
        }

        public void AddCallback(Action action)
        {
            packageList.OnLoaded -= action;
            packageList.OnLoaded += action;
        }


        public void StartSpinner()
        {
            if (loadingSpinner != null)
                loadingSpinner.Start();
        }

        public void StopSpinner()
        {
            if (loadingSpinner != null)
                loadingSpinner.Stop();
        }

        public void SetElementDisplay(VisualElement element, bool visible)
        {
            UIUtils.SetElementDisplay(element, visible);
            element.visible = visible;
        }

        static readonly Regex s_regRefs = new Regex("refs/(tags|remotes/origin)/([^/]+),(.+)$", RegexOptions.Compiled);
        static readonly Regex s_RepoUrl = new Regex("^([^@]+)@([^#]+)(#.+)?$", RegexOptions.Compiled);
        static readonly Regex s_VersionRef = new Regex(@"^([^\+]+)(\+.*)?$", RegexOptions.Compiled);

        public void InstallPackage(string packageName, string url, string version)
        {
            const string manifestPath = "Packages/manifest.json";
            var manifest = MiniJSON.Json.Deserialize(System.IO.File.ReadAllText(manifestPath)) as Dictionary<string, object>;
            var dependencies = manifest["dependencies"] as Dictionary<string, object>;

            dependencies.Remove(packageName);
            dependencies.Add(packageName, url + "#" + version);

            System.IO.File.WriteAllText(manifestPath, MiniJSON.Json.Serialize(manifest));
            UnityEditor.AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }



        public void UpdateCallback()
        {
            var pInfo = this.packageDetails.VersionPopup.value.Version;
            if (pInfo.Origin == (PackageSource)99)
            {
                var packageId = pInfo.Info.packageId;
                string url = s_RepoUrl.Replace(packageId, "$2");
                var m = s_VersionRef.Match(pInfo.Version.ToString());
                var version = 0 < m.Groups[2].Length
                    ? m.Groups[2].Value.TrimStart('+')
                    : m.Groups[1].Value;

                InstallPackage(pInfo.Name, url, version);
            }
            else
            {
                Expose.FromObject(packageDetails).Call("UpdateClick");
            }
        }


        bool reloading = false;

        public void UpdateGitPackages()
        {
            // On reloading package list, skip updating.
            if (reloading)
            {
                reloading = false;
                return;
            }

			// Get git packages.
            var gitPackages = GetPackages()
                .Where(x => x.Current.Origin == PackageSource.Git || x.Current.Origin == (PackageSource)99)
                .ToArray();

            if (gitPackages.Length == 0) return;

			// Start job.
            StartSpinner();
            HashSet<string> jobs = new HashSet<string>(gitPackages.Select(p => p.Current.Name));

            // Update
            foreach (var p in gitPackages)
            {
                var package = p;
                var pInfo = p.Current;
                pInfo.IsLatest = false;

                var packageName = pInfo.Name;
                pInfo.Origin = (PackageSource)99;
                var json = JsonUtility.ToJson(pInfo);
                var repoUrl = s_RepoUrl.Replace(pInfo.PackageId, "$2");
                Debug.Log($"{pInfo.Name} -> {repoUrl}");

				// Get available branch/tag names with package version. (e.g. "refs/tags/1.1.0,1.1.0")
                GitUtils.GetRefs(pInfo.Name, repoUrl, refsAndVersions =>
                {
                    UpdatePackageVersions(package, refsAndVersions);
                    jobs.Remove(packageName);
                    if (jobs.Count == 0)
                    {
                        UpdatePackageCollection();
                    }
                });
            }
        }

        void UpdatePackageVersions(Package package, IEnumerable<string> refs)
        {
            var pInfo = package.Current;
            var json = JsonUtility.ToJson(pInfo);
            var versions = refs
			.Select(r => s_regRefs.Match(r))
            .Where(m => m.Success)
            .Select(m =>
            {
				// [2]:branch/tag name, [3]:package version
                var ver = m.Groups[2].Value == m.Groups[3].Value
                    ? m.Groups[2].Value
                    : m.Groups[3].Value + "+" + m.Groups[2].Value;

                var newPInfo = JsonUtility.FromJson(json, typeof(PackageInfo)) as PackageInfo;
                newPInfo.Version = Semver.SemVersion.Parse(m.Groups[2].Value);
                newPInfo.IsCurrent = pInfo.Version == newPInfo.Version;
                newPInfo.IsVerified = newPInfo.IsCurrent;
                newPInfo.Origin = (PackageSource)99;
                newPInfo.Info = pInfo.Info;
                return newPInfo;
            })
            .ToArray();

            if (0 < versions.Length)
            {
                versions.OrderBy(v => v.Version).Last().IsLatest = true;
                package.UpdateSource(versions);
            }
        }

        void UpdatePackageCollection()
        {
            reloading = true;
            StopSpinner();
            Debug.LogFormat("[UpdateGitPackages] Reloading package collection...");
            var collection = PackageCollection.Instance;
            collection?.UpdatePackageCollection(false);
        }
    }
}


