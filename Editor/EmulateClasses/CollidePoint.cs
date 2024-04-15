using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class CollidePoint
    {
        public Hit hit { get; private set; }
        public object selfNode { get; private set; }

        public CollidePoint(
            Hit hit,
            object selfNode
        )
        {
            this.hit = hit;
            this.selfNode = selfNode;
        }
    }
}
