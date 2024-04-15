using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using ClusterVR.CreatorKit.Preview.PlayerController;

namespace Assets.KaomoLab.CSEmulator.Components
{
    [DisallowMultipleComponent, RequireComponent(typeof(VRM.VRMMeta))]
    public class CSEmulatorPlayerHandler
        : MonoBehaviour
    {
        public string id
        {
            get
            {
                //一旦UUIDにする。
                if (_id == null)
                    _id = Guid.NewGuid().ToString();
                return _id;
            }
        }
        string _id = null;

        public GameObject vrm => gameObject;

    }
}
