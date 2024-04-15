using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Components
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider))]
    public class CSEmulatorSubNodeHandler
        : MonoBehaviour
    {
        public CSEmulatorItemHandler parent { get; private set; }

        public void Construct(
            CSEmulatorItemHandler parent
        )
        {
            this.parent = parent;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CSEmulatorItemHandler.IsOverlapTarget(gameObject, other)) return;
            var (itemHandler, playerHandler) = CSEmulatorItemHandler.GetHandler(other);

            parent.SetSubNodeOverlap(gameObject.name, itemHandler, playerHandler);
        }

        private void OnTriggerStay(Collider other)
        {
            //必ずEnterが入るならいらない？
        }

        private void OnTriggerExit(Collider other)
        {
            if (!CSEmulatorItemHandler.IsOverlapTarget(gameObject, other)) return;
            var (itemHandler, playerHandler) = CSEmulatorItemHandler.GetHandler(other);

            parent.RemoveSubNodeOverlap(gameObject.name, itemHandler, playerHandler);
        }

        private void OnCollisionEnter(Collision collision)
        {
            parent.OnCollisionSubNode(gameObject.name, collision);
        }
    }
}
