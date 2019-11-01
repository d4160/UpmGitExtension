using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BridgeTest
{

    public class testtest
    {
        [UnityEditor.MenuItem("Packages/BridgeTest")]
        static void t()
        {
            var t = typeof(UnityEditor.PackageManager.UI.InternalBridge);
            Debug.Log(t);
            Debug.Log(t.Assembly.FullName);
            Debug.Log(t.AssemblyQualifiedName);
            // UnityEditor.PackageManager.UI.Bridge.Test.test();
            var o = UnityEditor.PackageManager.UI.InternalBridge.AssemblyTest2();
            Debug.Log(o);
            Debug.Log(o.GetType());
            Debug.Log(o.GetType().Assembly.FullName);
            Debug.Log(o.GetType().AssemblyQualifiedName);
        }
    }
}
