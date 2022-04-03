namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public interface IHaveStatus<out T>
    {
        public T GetStatus();
    }
}
