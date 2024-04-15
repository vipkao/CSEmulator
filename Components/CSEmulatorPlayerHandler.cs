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

        public Animator animator
        {
            get
            {
                if (_animator == null)
                    _animator = gameObject.GetComponent<Animator>();
                return _animator;
            }
        }
        Animator _animator = null;

        public event Handler OnUpdate = delegate { };

        private void Update()
        {
            OnUpdate.Invoke();
        }

    }
}
