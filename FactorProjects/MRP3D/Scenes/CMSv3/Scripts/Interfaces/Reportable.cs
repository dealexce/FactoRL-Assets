namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public interface IHasStatus<out T>
    {
        public T GetStatus();
    }
}
