namespace FactorProjects.MRP3D.Scenes.CMSv2.Scripts
{
    public interface IHasStatus<out T>
    {
        public T GetStatus();
    }
}
