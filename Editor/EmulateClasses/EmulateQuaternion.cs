﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class EmulateQuaternion
        : ISendableSize
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public EmulateQuaternion(
        ) :this(0, 0, 0, 0)
        {
        }

        public EmulateQuaternion(
            float x, float y, float z, float w
        )
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public EmulateQuaternion(
            UnityEngine.Quaternion source
        ) : this(source.x, source.y, source.z, source.w)
        {
        }

        public EmulateQuaternion(
            EmulateQuaternion source
        ) : this(source.x, source.y, source.z, source.w)
        {
        }

        public UnityEngine.Quaternion _ToUnityEngine()
        {
            return new UnityEngine.Quaternion(x, y, z, w);
        }

        void Apply(UnityEngine.Quaternion q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public EmulateQuaternion clone()
        {
            return new EmulateQuaternion(this);
        }

        public EmulateVector3 createEulerAngles()
        {
            return new EmulateVector3(_ToUnityEngine().eulerAngles);
        }

        public float dot(EmulateQuaternion q)
        {
            return UnityEngine.Quaternion.Dot(this._ToUnityEngine(), q._ToUnityEngine());
        }

        public bool equals(EmulateQuaternion q)
        {
            //Quaternionについても == operatorを使っていると思われる。
            return this._ToUnityEngine() == q._ToUnityEngine();
        }

        public EmulateQuaternion identity()
        {
            Apply(UnityEngine.Quaternion.identity);
            return this;
        }

        public EmulateQuaternion invert()
        {
            var inverted = UnityEngine.Quaternion.Inverse(_ToUnityEngine());
            Apply(inverted);
            return this;
        }

        public float length()
        {
            return (float)Math.Sqrt(lengthSq());
        }

        public float lengthSq()
        {
            return x*x + y*y + z*z + w*w;
        }


        public EmulateQuaternion multiply(EmulateQuaternion q)
        {
            //Quaternionのmultiplyだけ特殊
            Apply(_ToUnityEngine() * q._ToUnityEngine());
            return this;
        }

        public EmulateQuaternion normalize()
        {
            Apply(_ToUnityEngine().normalized);
            return this;
        }

        public EmulateQuaternion set(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
            return this;
        }

        public EmulateQuaternion setFromAxisAngle(EmulateVector3 axis, float degree)
        {
            var q = UnityEngine.Quaternion.AngleAxis(degree, axis._ToUnityEngine());
            Apply(q);
            return this;
        }

        public EmulateQuaternion setFromEulerAngles(EmulateVector3 v)
        {
            var q = UnityEngine.Quaternion.Euler(v._ToUnityEngine());
            Apply(q);
            return this;
        }

        public EmulateQuaternion slerp(EmulateQuaternion q, float a)
        {
            var slerped = UnityEngine.Quaternion.Slerp(
                this._ToUnityEngine(), q._ToUnityEngine(), a
            );
            Apply(slerped);
            return this;
        }

        public object toJSON(string key)
        {
            return this;
        }
        public override string ToString()
        {
            return String.Format("({0:f4},{1:f4},{2:f4},{3:f4})", x, y, z, w);
        }

        public int GetSize()
        {
            return 36;
        }
    }
}
