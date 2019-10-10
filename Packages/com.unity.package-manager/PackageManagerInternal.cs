using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;

namespace UnityEditor.PackageManager
{
    public class Internals
    {
        // [UnityEditor.InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            CompilationPipeline.assemblyCompilationFinished += (path, messages) =>
            {
                Debug.Log(path);
                Debug.Log(messages.Select(m=>m.message).Aggregate((a,b)=>a+"\n"+b));
                if (path == ("Library/ScriptAssemblies/Unity.InternalAPIEditorBridgeDev.001.dll"))
                {
                    File.Copy(path, "Packages/com.coffee.upm-git-extension/Editor/UnityEditor.PackageManager.Internals.dll", true);
                }
            };

        }

        public static void test()
        {
            Debug.Log(new PackageList());


            // UpmClient.instance.onPackagesChanged += OnPackagesChanged;
            // UpmClient.instance.onPackageVersionUpdated += OnUpmPackageVersionUpdated;
            // UpmClient.instance.onListOperation += OnUpmListOrSearchOperation;
        }
        
        [UnityEditor.InitializeOnLoadMethod]
        public static void Register()
        {
            UpmClient.instance.onPackagesChanged += x=>{
                Debug.Log($"UpmClient.instance.onPackagesChanged\n{x.Select(y=>y.uniqueId + ", " + y.GetType()).Aggregate((a,b)=>a+"\n"+b)}");
            };

            PackageDatabase.instance.onPackageOperationFinish += x=>
            {
                Debug.Log($"PackageDatabase.onPackageOperationFinish {x.uniqueId}");

            };

            PackageDatabase.instance.onRefreshOperationFinish += x=>{
                Debug.Log($"PackageDatabase.onRefreshOperationFinish {x}");
            };
        }

        [MenuItem("fugafuga/Reload")]
        public static void testfetch()
        {
            // UpmClient.instance.onPackageVersionUpdated();
            // PackageDatabase.instance.OnUpmPackageVersionUpdated("", );
            // UpmClient.instance.ExtraFetch();

            var package = PackageDatabase.instance.upmPackages.First(x=>x.uniqueId=="com.coffee.upm-git-extension");
            Debug.Log($"{package.GetType()}, {package.name}");
            
        }

    }
}
