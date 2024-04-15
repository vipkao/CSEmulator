using ClusterVR.CreatorKit.Preview.PlayerController;
using ClusterVR.CreatorKit.World.Implements.WorldRuntimeSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Components
{
    [DisallowMultipleComponent, RequireComponent(typeof(IPlayerController)), RequireComponent(typeof(CharacterController))]
    public class CSEmulatorPlayerController
        : MonoBehaviour
    {
        public class MovingPlatformSettings
        {
            public readonly bool UseMovingPlatform;
            public readonly bool MovingPlatformHorizontalInertia;
            public readonly bool MovingPlatformVerticalInertia;

            public MovingPlatformSettings(WorldRuntimeSetting worldRuntimeSetting)
            {
                UseMovingPlatform = worldRuntimeSetting?.UseMovingPlatform ?? WorldRuntimeSetting.DefaultValues.UseMovingPlatform;
                MovingPlatformHorizontalInertia = worldRuntimeSetting?.MovingPlatformHorizontalInertia ?? WorldRuntimeSetting.DefaultValues.MovingPlatformHorizontalInertia;
                MovingPlatformVerticalInertia = worldRuntimeSetting?.MovingPlatformVerticalInertia ?? WorldRuntimeSetting.DefaultValues.MovingPlatformVerticalInertia;
                if (UseMovingPlatform)
                    UnityEngine.Debug.Log("移動する床に追従する挙動はCSEmulatorにより少しは再現できているかもしれません。");
            }
        }

        const string RIGHT_HAND_UP = "IsRightHandUp";
        const string LEFT_HAND_UP = "IsLeftHandUp";
        const string WALKING = "IsWalking";
        const string DIRECTION_FORWARD = "DirectionForward";
        const string DIRECTION_RIGHT = "DirectionRight";
        const string MOVE_SPEED_RATIO = "SpeedRatio";

        public ICharacterController characterController { get; private set; } = null;
        public Vector3 velocity { get; private set; } = Vector3.zero;
        public float gravity { get; set; } = Commons.STANDARD_GRAVITY;
        bool isVelocityAdded = false;
        Vector3 movingPlatformHorizontalInertia = Vector3.zero;
        Vector3 movingPlatformVerticalInertia = Vector3.zero;

        Vector3 prevPosition = new Vector3(0, 0, 0);
        float baseSpeed;
        MovingAveragerFloat speedAverage = new MovingAveragerFloat(10);

        KeyWalkManager walkManager;
        BaseMoveSpeedManager speedManager;
        FaceConstraintManager faceConstraintManager;
        PlayerGroundingManager playerGroundingManager;

        public HumanPoseManager poseManager { get; private set; } = null;

        public Animator animator
        {
            get
            {
                if (_animator == null)
                    _animator = gameObject
                        .GetComponentInChildren<VRM.VRMMeta>()
                        .GetComponentInChildren<Animator>();
                return _animator;
            }
        }
        Animator _animator = null;

        MovingPlatformSettings movingPlatformSettings;
        IVelocityYHolder cckPlayerVelocityY;
        IBaseMoveSpeedHolder cckPlayerBaseMoveSpeed;
        IPlayerRotateHolder playerRotateHolder;
        IPerspectiveChangeNotifier perspectiveChangeNotifier;
        IRawInput rawInput;

        public void Construct(
            MovingPlatformSettings movingPlatformSettings,
            ICharacterController characterController,
            RuntimeAnimatorController animatorController,
            IVelocityYHolder cckPlayerVelocityY,
            IBaseMoveSpeedHolder cckPlayerBaseMoveSpeed,
            IPlayerRotateHolder playerRotateHolder,
            IPerspectiveChangeNotifier perspectiveChangeNotifier,
            IRawInput rawInput
        )
        {
            this.movingPlatformSettings = movingPlatformSettings;
            this.characterController = characterController;
            this.cckPlayerVelocityY = cckPlayerVelocityY;
            this.cckPlayerBaseMoveSpeed = cckPlayerBaseMoveSpeed;
            this.playerRotateHolder = playerRotateHolder;
            this.animator.runtimeAnimatorController = animatorController;
            this.walkManager = new KeyWalkManager(
                rawInput
            );
            this.baseSpeed = cckPlayerBaseMoveSpeed.value;
            this.speedManager = new BaseMoveSpeedManager(
                baseSpeed,
                //2.96実測値
                1.8f, 1.0f, 0.5f, rawInput
            );
            animator.SetFloat(MOVE_SPEED_RATIO, 1.0f);
            this.poseManager = new HumanPoseManager(
                animator
            );
            this.faceConstraintManager = new FaceConstraintManager(
                isFaceConstraintForward =>
                {
                    walkManager.ConstraintFaceForward(isFaceConstraintForward);
                    animator.SetBool("IsFaceForward", isFaceConstraintForward);
                    animator.SetTrigger("FaceConstraintChanged");
                }
            );
            this.perspectiveChangeNotifier = perspectiveChangeNotifier;
            perspectiveChangeNotifier.OnChanged += PerspectiveChangeNotifier_OnValueChanged;
            perspectiveChangeNotifier.RequestNotify();
            this.rawInput = rawInput;

            //slopeLimitとstepOffsetの挙動を見ると
            //CharacterControllerとRigidbody(+CapsuleCollider)の併用は
            //ほぼ間違いないように思える
            AddCapsuleCollider();

            playerGroundingManager = new PlayerGroundingManager(
                0.0001f,
                GetComponent<CharacterController>(),
                playerRotateHolder.rotateTransform
            );
        }
        private void AddCapsuleCollider()
        {
            var cc = GetComponent<CharacterController>();
            //Colliderが2つになる…
            //＞getOverlapsは、今(cluster2.95)のところ2つ返すのでOK。
            //＞getPlayersNearは、1つしか返さないので要調整。
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = cc.center;
            //cc側に衝突処理を持っていかれることがあるので+0.01
            //skinWidthより小さい値にしたのが良かったのかもしれない(未検証)
            col.height = cc.height + 0.01f;
            col.radius = cc.radius + 0.01f;
            var rb = gameObject.AddComponent<Rigidbody>();
            //この辺の設定をしておくとccと併用できそう
            rb.useGravity = false;
            rb.mass = 0;
            rb.drag = 0;
            rb.angularDrag = 0;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        private void PerspectiveChangeNotifier_OnValueChanged(bool data)
        {
            faceConstraintManager.ChangePerspective(data);
        }

        public void AddVelocity(Vector3 velocity)
        {
            this.velocity += velocity;
            isVelocityAdded = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            playerGroundingManager.AddCollision(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            playerGroundingManager.RemoveCollision(collision);
        }

        private void Update()
        {
            var p = gameObject.transform.position;
            var speedRate = (prevPosition - p).magnitude / Time.deltaTime / baseSpeed;
            //平均にしないとめっちゃ震えるので
            speedAverage.Push(speedRate);
            if(speedAverage.hasAverage)
                animator.SetFloat(MOVE_SPEED_RATIO, speedAverage.average);
            prevPosition = p;
        }

        private void LateUpdate()
        {
            //OnUpdateといった形でeventを外に出してはいけないとする。
            //複数インスタンスがOnUpdateを購読した場合
            //そのインスタンス数分だけ処理が重複してしまう可能性がある。
            //その危険性を排除したい。

            ApplyMovingPlatform(); //velocity弄るので最初
            ApplyAdditionalGravity();
            ApplyAdditionalVelocity();
            ApplyBaseMoveSpeed();
            //https://docs.unity3d.com/ja/2019.4/ScriptReference/CharacterController.SimpleMove.html
            //1フレームで複数回呼ぶのを推奨していないけどもしかたない
            characterController.Move(velocity * Time.deltaTime);

            //歩きモーションの後にポーズを上書きするという挙動
            //つまり歩きモーションがポーズのリセット兼ねている
            ApplyAnimation();
            poseManager.Apply();
        }
        void ApplyMovingPlatform()
        {
            if (!movingPlatformSettings.UseMovingPlatform) return;

            playerGroundingManager.UpdateGrounded((isGrounded, delta) =>
            {
                if (!isGrounded) return;
                gameObject.transform.position += delta.position;
                prevPosition += delta.position;
                playerRotateHolder.rotateTransform.rotation = Quaternion.Euler(
                    0, (delta.rotation * playerRotateHolder.rotateTransform.rotation).eulerAngles.y, 0
                );
            });
            if (playerGroundingManager.IsTakingOff())
            {
                movingPlatformHorizontalInertia = playerGroundingManager.GetHorizontalInertia();
                movingPlatformVerticalInertia = playerGroundingManager.GetVerticalInertia();
                if (!movingPlatformSettings.MovingPlatformHorizontalInertia)
                    movingPlatformHorizontalInertia = Vector3.zero;
                if (!movingPlatformSettings.MovingPlatformVerticalInertia)
                    movingPlatformVerticalInertia = Vector3.zero;
                velocity += movingPlatformHorizontalInertia;
                velocity += movingPlatformVerticalInertia;
                if(movingPlatformHorizontalInertia != Vector3.zero || movingPlatformVerticalInertia != Vector3.zero)
                    isVelocityAdded = true;
            }
            if (playerGroundingManager.IsGrounding())
            {
                //CCK2.11.0着地時に移動床からの慣性分はキャンセルされるっぽい
                if (movingPlatformHorizontalInertia == Vector3.zero && movingPlatformVerticalInertia == Vector3.zero)
                    return;
                velocity -= movingPlatformHorizontalInertia;
                velocity -= movingPlatformVerticalInertia;
                movingPlatformHorizontalInertia = Vector3.zero;
                movingPlatformVerticalInertia = Vector3.zero;
            }
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
            //抵抗の係数。接地してたら摩擦の方で大きくなるという発想。
            //現地調査の結果これが一番近い。
            //調査方法：垂直飛び、接地からの横、上空からの横の３パターンで到達点を比較。
            var k = characterController.isGrounded ? 0.013f : 0.00f;
            velocity -= velocity * k;

            if (!isVelocityAdded && characterController.isGrounded)
            {
                //着地した時に上方向の速度が残っていると跳ねる。
                velocity = new Vector3(velocity.x, 0, velocity.z);
            }
            isVelocityAdded = false;

        }
        void ApplyBaseMoveSpeed()
        {
            speedManager.Update(
                speed =>
                {
                    cckPlayerBaseMoveSpeed.value = speed;
                }
            );
        }
        void ApplyAnimation()
        {
            if (Input.GetKeyDown(KeyCode.C))
                animator.SetBool(RIGHT_HAND_UP, true);
            if (Input.GetKeyUp(KeyCode.C))
                animator.SetBool(RIGHT_HAND_UP, false);

            if (Input.GetKeyDown(KeyCode.Z))
                animator.SetBool(LEFT_HAND_UP, true);
            if (Input.GetKeyUp(KeyCode.Z))
                animator.SetBool(LEFT_HAND_UP, false);

            walkManager.Update(
                forwardDirection => animator.SetFloat(DIRECTION_FORWARD, forwardDirection),
                rightDirection => animator.SetFloat(DIRECTION_RIGHT, rightDirection)
            );

        }

        public void ChangeGrabbing(bool isGrab)
        {
            faceConstraintManager.ChangeGrabbing(isGrab);
        }
        public void ChangePerspective(bool isFirstPerson)
        {
            faceConstraintManager.ChangePerspective(isFirstPerson);
        }

        public void ForceForward()
        {
            walkManager.ForceForward();
        }

        public Quaternion ApplyDirection(Quaternion source)
        {
            if (faceConstraintManager.isConstraintForward)
            {
                return source;
            }
            var d = walkManager.GetDirectionAngle();
            var ret = source * Quaternion.Euler(0, d, 0);
            return ret;
        }

        private void OnDestroy()
        {
            perspectiveChangeNotifier.OnChanged -= PerspectiveChangeNotifier_OnValueChanged;
        }
    }
}
