using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.UI;

namespace hogehogehoge
{

    public class Bubuka
    {
        // Start is called before the first frame update
        [MenuItem("fuga/fuga")]
        static void Start()
        {
            Debug.Log(typeof(InternalBridge).AssemblyQualifiedName);
            InternalBridge.Instance.UpdateGitPackages();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}