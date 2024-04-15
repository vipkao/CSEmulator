using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class ApiAudio
    {
        //structなのでnullではなく、各値のデフォルト値が入っている。
        readonly ClusterVR.CreatorKit.Item.ItemAudioSet itemAudioSet;

        readonly UnityEngine.GameObject root;
        readonly UnityEngine.GameObject gameObject;
        readonly UnityEngine.AudioSource audioSource;

        //1を超えて値を保持できるように
        float _volume;

        public ApiAudio(
            ClusterVR.CreatorKit.Item.ItemAudioSet itemAudioSet,
            UnityEngine.GameObject root
        )
        {
            this.itemAudioSet = itemAudioSet;
            this.root = root;

            //ScriptableItemの子のような挙動をする。
            gameObject = new UnityEngine.GameObject();
            gameObject.transform.parent = root.transform;
            gameObject.transform.localPosition = UnityEngine.Vector3.zero;
            gameObject.transform.localRotation = UnityEngine.Quaternion.identity;
            gameObject.transform.localScale = UnityEngine.Vector3.one;

            this.audioSource = gameObject.AddComponent<UnityEngine.AudioSource>();
            audioSource.clip = itemAudioSet.AudioClip;
            audioSource.loop = itemAudioSet.Loop;
            audioSource.dopplerLevel = 0;
            audioSource.spatialBlend = 1.0f;
            _volume = audioSource.volume;
        }

        public float volume
        {
            get
            {
                return _volume;
            }
            set
            {
                //CSETODO audioSourceは1を超えて設定できないので、別の変数に入れおく。
                //なにかいい方法を見つける？
                _volume = Math.Clamp(value, 0f, 2.5f);
                audioSource.volume = value;
            }
        }

        public void attach(SubNode subNode)
        {
            gameObject.transform.localPosition = subNode.getPosition()._ToUnityEngine();
        }

        public void attachToRoot()
        {
            gameObject.transform.localPosition = UnityEngine.Vector3.zero;
        }

        public void play()
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            audioSource.Play();
        }

        public void stop()
        {
            audioSource.Stop();
        }

        public object toJSON(string key)
        {
            return this;
        }
        public override string ToString()
        {
            var clip = itemAudioSet.AudioClip;
            return String.Format("[ApiAudio][{0}:{1}]", itemAudioSet.Id, clip == null ? null : clip.name);
        }
    }
}
