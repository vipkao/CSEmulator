using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class Collision
    {
        readonly IEnumerable<CollidePoint> _collidePoints;
        //この配列を直接書き換えようという発想はまあ無いだろうという前提
        public CollidePoint[] collidePoints { get => _collidePoints.ToArray(); }

        public readonly EmulateVector3 impulse;
        public readonly HitObject @object;
        public readonly EmulateVector3 relativeVelocity;

        public Collision(
            IEnumerable<CollidePoint> collidePoints,
            EmulateVector3 impulse,
            HitObject hitObject,
            EmulateVector3 relativeVelocity
        )
        {
            this._collidePoints = collidePoints;
            this.impulse = impulse;
            this.@object = hitObject;
            this.relativeVelocity = relativeVelocity;
        }
    }
}
