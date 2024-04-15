using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    public class NearDetectorPreparer
    {
        public Components.CSEmulatorNearDetectorHandler InstantiateHandler(GameObject parent)
        {
            var nearDetector = GameObject.Instantiate(
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/KaomoLab/CSEmulator/Components/CSEmulatorNearDetector.prefab"
                ),
                parent.transform
            );
            var handler = nearDetector.GetComponent<Components.CSEmulatorNearDetectorHandler>();
            return handler;
        }
    }
}
