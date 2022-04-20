using System.Collections.Generic;
using JetBrains.Annotations;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public interface ISchedule<T>
    {
        public void ScheduleNew([NotNull] List<T> newSchedule);
        public void ScheduleAppend(T appendTask);
        public bool TryPushSchedule();
        public T LastAppendedTask();
        public int ScheduleCount();
    }
}