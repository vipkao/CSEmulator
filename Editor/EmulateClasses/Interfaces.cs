using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public interface IUpdateListenerBinder
    {
        void SetUpdateCallback(string key, string source, Action<double> Callback);
        void DeleteUpdateCallback(string key);
    }

    public interface IReceiveListenerBinder
    {
        void SetReceiveCallback(Components.CSEmulatorItemHandler owner, Action<string, object, ItemHandle> Callback);
    }

    public interface IMessageSender
    {
        void send(ulong id, string requestName, object arg, Components.CSEmulatorItemHandler sender);
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

        UnityEngine.Vector3 getPosition();
        UnityEngine.Quaternion getRotation();
        void setPosition(UnityEngine.Vector3 position);
        void setRotation(UnityEngine.Quaternion rotation);
    }

}
