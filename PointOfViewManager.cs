using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator
{
    public class PointOfViewManager
    {
        readonly GameObject firstPersonCameraObject;
        public Camera firstPersonCamera { get; private set; }
        public UnityEngine.Rendering.PostProcessing.PostProcessLayer firstPersonPpl { get; private set; }

        readonly GameObject thridPersonCameraObject;
        public Camera thirdPersonCamera { get; private set; }
        public UnityEngine.Rendering.PostProcessing.PostProcessLayer thridPersonPpl { get; private set; }

        readonly float cameraDistance;
        readonly Collider[] ignoreColliders;
        readonly Action<Camera> ToFirstPersonCallback;
        readonly Action<Camera> ToThridPersonCallback;

        readonly RaycastHit[] raycastHits = new RaycastHit[5];

        public PointOfViewManager(
            float cameraDistance,
            GameObject firstPersonCameraObject,
            Collider[] ignoreColliders,
            Action<Camera> ToFirstPersonCallback,
            Action<Camera> ToThridPersonCallback
        )
        {
            this.cameraDistance = cameraDistance;
            this.firstPersonCameraObject = firstPersonCameraObject;
            this.firstPersonCamera = firstPersonCameraObject.GetComponent<Camera>();
            this.firstPersonPpl = firstPersonCameraObject.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
            this.ignoreColliders = ignoreColliders;
            this.ToFirstPersonCallback = ToFirstPersonCallback;
            this.ToThridPersonCallback = ToThridPersonCallback;

            var root = new GameObject("CSEmulatorThridPersonCameraRoot");
            root.transform.parent = firstPersonCameraObject.transform;
            root.transform.localPosition = Vector3.zero;

            thridPersonCameraObject = new GameObject("CSEmulatorThridPersonCamera");
            thirdPersonCamera = thridPersonCameraObject.AddComponent<Camera>();
            thridPersonCameraObject.transform.parent = root.transform;
            thirdPersonCamera.enabled = false;

            thirdPersonCamera.CopyFrom(firstPersonCamera);
            SetThridPersonCameraPosition(cameraDistance);

            thridPersonPpl = thridPersonCameraObject.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
            thridPersonPpl.volumeLayer = (1 << 21); //PostProcessing
            thridPersonPpl.volumeTrigger = thridPersonCameraObject.transform;
        }

        void SetThridPersonCameraPosition(float distance)
        {
            thridPersonCameraObject.transform.localPosition = new Vector3(0, 0, -distance);
        }

        public void UpdateThridPersonCameraPosition()
        {
            var hitCount = Physics.RaycastNonAlloc(
                firstPersonCameraObject.transform.position,
                -(firstPersonCameraObject.transform.rotation * Vector3.forward),
                raycastHits,
                cameraDistance,
                -1,
                QueryTriggerInteraction.Ignore
            );

            if (hitCount == 0)
            {
                SetThridPersonCameraPosition(cameraDistance);
                return;
            }

            var validHits = raycastHits
                .Take(hitCount)
                .Where(h => !ignoreColliders.Contains(h.collider))
                .OrderBy(h => h.distance)
                .ToArray();

            if (validHits.Length == 0)
            {
                SetThridPersonCameraPosition(cameraDistance);
                return;
            }

            SetThridPersonCameraPosition(validHits[0].distance);
        }


        public void ChangeView(bool isFirstPerson)
        {
            if (isFirstPerson)
            {
                thirdPersonCamera.enabled = false;
                thridPersonPpl.enabled = false; //よくわからないタイミングでPPLがエラーになる問題に、これで対応できているか分からない。
                firstPersonCamera.enabled = true;
                firstPersonPpl.enabled = true;
                ToFirstPersonCallback(firstPersonCamera);
            }
            else
            {
                thirdPersonCamera.enabled = true;
                thridPersonPpl.enabled = true;
                firstPersonCamera.enabled = false;
                firstPersonPpl.enabled = false;
                ToThridPersonCallback(thirdPersonCamera);
            }
        }
    }
}
