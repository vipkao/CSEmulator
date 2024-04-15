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

    public interface IPlayerHandleHolder
    {
        Components.CSEmulatorPlayerHandler GetById(string id);
        //ownerのidが取得できるようになったら不要
        Components.CSEmulatorPlayerHandler GetOwner();
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

}
