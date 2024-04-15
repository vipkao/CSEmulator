using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Components
{
    [DisallowMultipleComponent, RequireComponent(typeof(ClusterVR.CreatorKit.Item.Implements.Item))]
    public class CSEmulatorPrefabItem
         : MonoBehaviour
    {
        [SerializeField] public string _id;

        public string id { get => _id.ToLowerInvariant(); }
    }
}
