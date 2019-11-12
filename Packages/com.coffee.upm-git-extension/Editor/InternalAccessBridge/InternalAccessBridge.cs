using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager.UI;
using System.Text.RegularExpressions;
using System.IO;
using Semver;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace UnityEditor.PackageManager.UI
{
    public static class ButtonExtension
    {
        public static void OverwriteCallback(this Button button, Action action)
        {
            button.RemoveManipulator(button.clickable);
            button.clickable = new Clickable(action);
            button.AddManipulator(button.clickable);
        }
    }

    public class InternalBridge
    {

        private static InternalBridge instance = new InternalBridge();
        public static InternalBridge Instance { get { return instance; } }


        LoadingSpinner loadingSpinner = null;
        PackageList packageList = null;
        PackageDetails packageDetails = null;

        // Button ViewDocButton = null;
        // Button ViewChangelogButton = null;
        // Button ViewLicensesButton = null;
        // Button ViewReadmeButton = null;

        PackageInfo DisplayPackage { get { return this.packageDetails.VersionPopup.value.Version; } }


        // string GetFilePath(PackageInfo packageInfo, string filePattern)
        // {
        //     Debug.Log(packageInfo.Info.resolvedPath);
        //     return packageInfo != null
        //         ? GetFilePath(packageInfo.Info.resolvedPath, filePattern)
        //         : "";
        // }

        // string GetFilePath(string resolvedPath, string filePattern)
        // {
        //     if (string.IsNullOrEmpty(resolvedPath) || string.IsNullOrEmpty(filePattern))
        //         return "";

        //     foreach (var path in Directory.GetFiles(resolvedPath, filePattern))
        //     {
        //         if (!path.EndsWith(".meta", StringComparison.Ordinal))
        //         {
        //             return path;
        //         }
        //     }
        //     return "";
        // }


        private InternalBridge() { }

        public void Setup(VisualElement loadingSpinner, VisualElement packageList, VisualElement packageDetails)
        {
            this.loadingSpinner = loadingSpinner as LoadingSpinner;
            this.packageList = packageList as PackageList;
            this.packageDetails = packageDetails as PackageDetails;

            // this.ViewDocButton = packageDetails.Q<Button>("viewDocumentation");
            // this.ViewChangelogButton = packageDetails.Q<Button>("viewChangelog");
            // this.ViewLicensesButton = packageDetails.Q<Button>("viewLicenses");

            // var hostButton = packageDetails.Q<Button>("hostButton");
            // if (hostButton == null)
            // {
            //     hostButton = new Button() { text = "hostButton", name = "hostButton", tooltip = "View on browser" };
            //     hostButton.RemoveFromClassList("unity-button");
            //     hostButton.RemoveFromClassList("button");
            //     packageDetails.Q("detailVersion").parent.Add(hostButton);
            // }

            // ViewDocButton.OverwriteCallback(()=>ViewDocmentationClick("README.*", DisplayPackage.GetDocumentationUrl));
            // ViewChangelogButton.OverwriteCallback(()=>ViewDocmentationClick("CHANGELOG.*", DisplayPackage.GetChangelogUrl));
            // ViewLicensesButton.OverwriteCallback(()=>ViewDocmentationClick("LICENSE.*", DisplayPackage.GetLicensesUrl));
            // ViewChangelogButton.OverwriteCallback(ViewChangelogClick);
            // ViewLicensesButton.OverwriteCallback(ViewLicensesClick);

            this.packageList.OnLoaded -= UpdateGitPackages;
            this.packageList.OnLoaded += UpdateGitPackages;
        }

        // void OverwriteClickable(Button button, Action action)
        // {
        //     button.RemoveManipulator(button.clickable);
        //     button.clickable = new Clickable(action);
        //     button.AddManipulator(button.clickable);
        // }

        void ViewDocmentationClick(string filePattern, Action<string> action, Func<string> defaultFunc)
        {
            if (DisplayPackage.Info.source == PackageSource.Git)
            {
                action(PackageUtilsXXX.GetFilePath(DisplayPackage.Info.resolvedPath, filePattern));
            }
            else
            {
                Application.OpenURL(defaultFunc());
            }
        }

        // public void ViewDocClick(Action<string> action)
        // {
        //     ViewDocmentationClick("README.*", DisplayPackage.GetDocumentationUrl);
        // }

        public void ViewDocClick(Action<string> action)
        {
            ViewDocmentationClick("README.*", action, DisplayPackage.GetDocumentationUrl);
        }
        public void ViewChangelogClick(Action<string> action)
        {
            ViewDocmentationClick("CHANGELOG.*", action, DisplayPackage.GetChangelogUrl);
        }
        public void ViewLicensesClick(Action<string> action)
        {
            ViewDocmentationClick("LICENSE.*", action, DisplayPackage.GetLicensesUrl);
        }

        public void ViewRepoClick()
        {
            Application.OpenURL(PackageUtilsXXX.GetRepoHttpsUrl(DisplayPackage.Info.packageId));
        }


        // void ViewChangelogClick()
        // {
        //     if (DisplayPackage.Info.source == PackageSource.Git)
        //     {
        //         MarkdownUtils.OpenInBrowser(GetFilePath(DisplayPackage, "CHANGELOG.*"));
        //     }
        //     else
        //     {
        //         Application.OpenURL(DisplayPackage.GetDocumentationUrl());
        //     }
        // }


        // void ViewLicensesClick()
        // {
        //     if (DisplayPackage.Info.source == PackageSource.Git)
        //     {
        //         MarkdownUtils.OpenInBrowser(GetFilePath(DisplayPackage, "LICENSE.*"));
        //     }
        //     else
        //     {
        //         Application.OpenURL(DisplayPackage.GetDocumentationUrl());
        //     }
        // }


        static IEnumerable<Package> GetAllPackages()
        {
            var collection = PackageCollection.Instance;
            return collection?.LatestListPackages
                .Select(x => x.Name)
                .Distinct()
                .Select(collection.GetPackageByName)
                .Distinct() ?? Enumerable.Empty<Package>();
        }

        // public void AddCallback(Action action)
        // {
        //     // packageList.OnLoaded -= action;
        //     // packageList.OnLoaded += action;
        // }


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

        // public void SetElementDisplay(VisualElement element, bool visible)
        // {
        //     UIUtils.SetElementDisplay(element, visible);
        //     element.visible = visible;
        // }

        // static readonly Regex s_regRefs = new Regex("refs/(tags|remotes/origin)/([^/]+),(.+)$", RegexOptions.Compiled);
        static readonly Regex s_RepoUrl = new Regex("^([^@]+)@([^#]+)(#.+)?$", RegexOptions.Compiled);
        // static readonly Regex s_VersionRef = new Regex(@"^(.+)---(.*)?$", RegexOptions.Compiled);

        // public void InstallPackage(string packageName, string url, string refName)
        // {
        //     const string manifestPath = "Packages/manifest.json";
        //     var manifest = MiniJSON.Json.Deserialize(System.IO.File.ReadAllText(manifestPath)) as Dictionary<string, object>;
        //     var dependencies = manifest["dependencies"] as Dictionary<string, object>;

        //     dependencies.Add(packageName, url + "#" + refName);

        //     System.IO.File.WriteAllText(manifestPath, MiniJSON.Json.Serialize(manifest));
        //     UnityEditor.EditorApplication.delayCall += () => UnityEditor.AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        // }

        // public void RemovePackage(string packageName)
        // {
        //     const string manifestPath = "Packages/manifest.json";
        //     var manifest = MiniJSON.Json.Deserialize(System.IO.File.ReadAllText(manifestPath)) as Dictionary<string, object>;
        //     var dependencies = manifest["dependencies"] as Dictionary<string, object>;

        //     dependencies.Remove(packageName);

        //     System.IO.File.WriteAllText(manifestPath, MiniJSON.Json.Serialize(manifest));
        //     UnityEditor.EditorApplication.delayCall += () => UnityEditor.AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        // }

        public void UpdateClick()
        {
            var packageInfo = DisplayPackage;
            if (packageInfo.Info.source == PackageSource.Git)
            {
                string packageId = packageInfo.Info.packageId;
                string url = s_RepoUrl.Replace(packageId, "$2");
                string refName = packageInfo.PackageId.Split('@')[1];
                PackageUtilsXXX.RemovePackage(packageInfo.Name);
                PackageUtilsXXX.InstallPackage(packageInfo.Name, url, refName);
            }
            else
            {
                Expose.FromObject(packageDetails).Call("UpdateClick");
            }
        }

        public void RemoveClick()
        {
            var packageInfo = DisplayPackage;
            if (packageInfo.Info.source == PackageSource.Git)
            {
                PackageUtilsXXX.RemovePackage(packageInfo.Name);
            }
            else
            {
                Expose.FromObject(packageDetails).Call("RemoveClick");
            }
        }

        int frameCount = 0;
        bool reloading;

        public void UpdateGitPackages()
        {

            // Debug.Log(" ============= UpdateGitPackages skip=" + reloading);
            // On reloading package list, skip updating.
            if (reloading) return;

            // Get git packages.
            var gitPackages = GetAllPackages()
                .Where(x => x.Current.Origin == PackageSource.Git || x.Current.Origin == (PackageSource)99)
                .ToArray();

            if (gitPackages.Length == 0) return;

            // Start job.
            // StartSpinner();
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
                // Debug.Log($"{pInfo.Name} -> {repoUrl}");

                // Get available branch/tag names with package version. (e.g. "refs/tags/1.1.0,1.1.0")
                GitUtils.GetRefs(pInfo.Name, repoUrl, refs =>
                {
                    UpdatePackageVersions(package, refs);
                    jobs.Remove(packageName);
                    if (jobs.Count == 0)
                    {
                        // StopSpinner();
                        frameCount = Time.frameCount;
                        reloading = true;
                        UpdatePackageCollection();
                        reloading = false;

                    }
                });
            }
        }

        void UpdatePackageVersions(Package package, IEnumerable<string> versions)
        {
            var pInfo = package.Current;
            var json = JsonUtility.ToJson(pInfo);
            var versionInfos = versions
                .Select(ver =>
                {
                    var splited = ver.Split(',');
                    var refName = splited[0];
                    var version = splited[1];
                    var newPInfo = JsonUtility.FromJson(json, typeof(PackageInfo)) as PackageInfo;

                    newPInfo.Version = SemVersion.Parse(version == refName ? version : version + "-" + refName);
                    newPInfo.IsCurrent = false;
                    newPInfo.IsVerified = false;
                    newPInfo.Origin = (PackageSource)99;
                    newPInfo.Info = pInfo.Info;
                    newPInfo.PackageId = string.Format("{0}@{1}", newPInfo.Name, refName);
                    return newPInfo;
                })
                .Concat(new[] { pInfo })
                .Where(p => p == pInfo || p.Version != pInfo.Version)
                .ToArray();

            if (0 < versionInfos.Length)
            {
                versionInfos.OrderBy(v => v.Version).Last().IsLatest = true;
                package.UpdateSource(versionInfos);
            }
        }

        void UpdatePackageCollection()
        {
            // Debug.LogFormat("[UpdateGitPackages] Reloading package collection...");
            var collection = PackageCollection.Instance;
            collection?.UpdatePackageCollection(false);
        }
    }
}


