using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public interface IUpdateListenerBinder
    {
        void SetUpdateCallback(string key, UnityEngine.GameObject source, Action<double> Callback);
        void DeleteUpdateCallback(string key);
        void SetLateUpdateCallback(string key, UnityEngine.GameObject source, Action<double> Callback);
        void DeleteLateUpdateCallback(string key);
    }

    public interface IReceiveListenerBinder
    {
        void SetReceiveCallback(Components.CSEmulatorItemHandler owner, Action<string, object, ItemHandle> Callback);
        void DeleteReceiveCallback(Components.CSEmulatorItemHandler owner);
    }

    public interface IMessageSender
    {
        void Send(ulong id, string requestName, object arg, Components.CSEmulatorItemHandler sender);
    }

    public interface IPrefabItemHolder
    {
        UnityEngine.GameObject GetPrefab(string uuid);
    }

    //CSETODO itemをinteractされたとき、そのplayerをどう取得する？できる？
    //それが解決するまでの仮
    public interface IItemOwnerHandler
    {
        string GetOwnerId();
    }

    public interface IPlayerMeta
    {
        string userId { get; }
        string userDisplayName { get; }
        bool exists { get; }
    }
    public interface IPlayerMetaHolder
    {
        IPlayerMeta GetById(string id);
    }
    public interface IPlayerHandleFactory
    {
        PlayerHandle CreateById(string id, Components.CSEmulatorItemHandler csOwnerItemHandler);
    }

    public interface IPlayerControllerFactory
    {
        IPlayerController Create(CSEmulator.Components.CSEmulatorPlayerHandler csPlayerHandler);
    }

    public interface IPlayerController
    {
        UnityEngine.Animator animator { get; }
        UnityEngine.Transform transform { get; }
        UnityEngine.GameObject vrm { get; }

        string id { get; }
        bool exists { get; }

        float gravity { get;  set; }
        float jumpSpeedRate { set; }
        float moveSpeedRate { set; }

        void Respawn();

        void AddVelocity(UnityEngine.Vector3 velocity);

        UnityEngine.Vector3 GetPosition();
        UnityEngine.Quaternion GetRotation();
        void SetPosition(UnityEngine.Vector3 position);
        void SetRotation(UnityEngine.Quaternion rotation);

        void SetHumanPosition(UnityEngine.Vector3? position);
        void SetHumanRotation(UnityEngine.Quaternion? rotation);
        void SetHumanMuscles(float[] muscles, bool[] hasMascles);
        void InvalidateHumanMuscles();
        UnityEngine.HumanPose GetHumanPose();

        void ChangeGrabbing(bool isGrab);
        void ChangePerspective(bool isFirstPerson);
    }

    public interface IUserInterfaceHandler
    {
        bool isTextInputting { get; }
        void StartTextInput(string caption, Action<string> SendCallback, Action CancelCallback, Action BusyCallback);
    }

    public interface ITextInputListenerBinder
    {
        void SetReceiveCallback(Components.CSEmulatorItemHandler owner, Action<string, string, TextInputStatus> Callback);
        void DeleteReceiveCallback(Components.CSEmulatorItemHandler owner);
    }

    public interface ITextInputSender
    {
        void Send(ulong id, string text, string meta, TextInputStatus status);
    }

    public interface IItemLifecycler
    {
        ClusterVR.CreatorKit.Item.IItem CreateItem(
            ItemTemplateId itemTemplateId,
            EmulateVector3 position,
            EmulateQuaternion rotation
        );
        void DestroyItem(ClusterVR.CreatorKit.Item.IItem item);
    }

    public interface ICckComponentFacade
    {
        /// <summary>
        /// bool isLeftHand
        /// bool isGrab true:Grab false:Release
        /// </summary>
        event Handler<bool, bool> onGrabbed;

        /// <summary>
        /// bool isOn
        /// </summary>
        event Handler<bool> onRide;

        /// <summary>
        /// bool isDown
        /// </summary>
        event Handler<bool> onUse;

        event Handler onInteract;
        void AddInteractItemTrigger();

        bool isGrab { get; }
        bool hasCollider { get; }
        bool hasGrabbableItem { get; }
        bool hasRidableItem { get; }

        public void SendSignal(string target, string key);
        public void SetState(string target, string key, object value);
        public object GetState(string target, string key, string parameterType);
    }
    public interface IExternalCaller
    {
        void CallExternal(string request, string meta);
        void SetCallEndCallback(Action<string, string, string> Callback);
    }
    public interface ICckComponentFacadeFactory
    {
        ICckComponentFacade Create(UnityEngine.GameObject gameObject);
    }

}
