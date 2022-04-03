using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public interface IResettable
    {
        public Vector3 InitPosition { get; set; }
        public void EpisodeReset();
    }
}
