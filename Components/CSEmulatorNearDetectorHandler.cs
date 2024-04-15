using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Components
{
    public class CSEmulatorNearDetectorHandler
        : MonoBehaviour
    {
        public void SetDetector(Vector3 position, float radius)
        {
            var items = Physics.OverlapSphere(
                position, radius,
                Commons.BuildLayerMask(1, 11, 14, 18) //Default, RidingItem, InteractableItem, GrabbingItem
            )
                .Select(c => c.gameObject.GetComponent<ClusterVR.CreatorKit.Item.IItem>())
                .Where(c => c != null).ToArray();

        }
    }
}
