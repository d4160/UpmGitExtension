using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager.UI;
using LibGit2Sharp;

namespace UnityEditor.PackageManager.UI
{
    public class InternalBridge
    {


        public InternalBridge()
        {

        }


        static IEnumerable<Package> GetPackages()
        {
            var collection = PackageCollection.Instance;
            return collection?.LatestListPackages
                .Select(x => collection.GetPackageByName(x.Name))
                .Distinct() ?? Enumerable.Empty<Package>();
        }

        public static void AddCallback(object element, Action action)
        {
            var packageList = element as PackageList;
            packageList.OnLoaded += action;
        }


        public static string GetSelectedPackage()
        {
            var collection = PackageCollection.Instance;
            return collection?.SelectedPackage;
        }


        public static object AssemblyTest()
        {
            return new LibGit2Sharp.CloneOptions();
        }

        public static object AssemblyTest2()
        {
            return new PackageInfo(
                
            );
        }
    }
}


