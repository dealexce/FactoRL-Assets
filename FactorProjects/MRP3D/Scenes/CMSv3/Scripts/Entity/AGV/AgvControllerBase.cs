using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class AgvControllerBase : MonoBehaviour, IManualInit<Agv>
    {
        public Agv Agv { get; private set; }
        public virtual void Init(Agv model)
        {
            this.Agv = model;
        }
    }
}
