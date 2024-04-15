using ClusterVR.CreatorKit.Preview.PlayerController;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Components
{
    [DisallowMultipleComponent, RequireComponent(typeof(IPlayerController))]
    public class CSEmulatorPlayerController
        : MonoBehaviour
    {
        public CharacterController characterController { get; private set; } = null;
        public Vector3 velocity { get; private set; } = Vector3.zero;
        public float gravity { get; set; } = Commons.STANDARD_GRAVITY;
        bool isAdded = false;

        IVelocityYHolder cckPlayerVelocityY;

        public void Construct(
            CharacterController characterController,
            IVelocityYHolder cckPlayerVelocityY
        )
        {
            this.characterController = characterController;
            this.cckPlayerVelocityY = cckPlayerVelocityY;
        }

        public void AddVelocity(Vector3 velocity)
        {
            this.velocity += velocity;
            isAdded = true;
        }

        private void LateUpdate()
        {
            //OnUpdateといった形でeventを外に出してはいけないとする。
            //複数インスタンスがOnUpdateを購読した場合
            //そのインスタンス数分だけ処理が重複してしまう可能性がある。
            //その危険性を排除したい。

            ApplyAdditionalGravity();
            ApplyAdditionalVelocity();
        }
        void ApplyAdditionalGravity()
        {
            if (gravity != CSEmulator.Commons.STANDARD_GRAVITY)
            {
                //DesktopPlayerControllerの重力加速度が決め打ちなので
                //ここで追計算する必要がある。
                var delta = Time.deltaTime * (gravity - CSEmulator.Commons.STANDARD_GRAVITY);
                cckPlayerVelocityY.value += delta;
            }
        }
        void ApplyAdditionalVelocity()
        {
            //https://docs.unity3d.com/ja/2019.4/ScriptReference/CharacterController.SimpleMove.html
            //1フレームで複数回呼ぶのを推奨していないけどもしかたない

            //抵抗の係数。接地してたら摩擦の方で大きくなるという発想。
            //現地調査の結果これが一番近い。
            //調査方法：垂直飛び、接地からの横、上空からの横の３パターンで到達点を比較。
            var k = characterController.isGrounded ? 0.013f : 0.00f;
            velocity -= velocity * k;

            if (!isAdded && characterController.isGrounded)
            {
                //着地した時に上方向の速度が残っていると跳ねる。
                velocity = new Vector3(velocity.x, 0, velocity.z);
            }
            isAdded = false;

            characterController.Move(velocity * Time.deltaTime);
        }
    }
}
