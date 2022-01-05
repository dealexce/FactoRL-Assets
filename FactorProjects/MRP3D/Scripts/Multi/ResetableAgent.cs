using Unity.MLAgents;

namespace Multi
{
    public abstract class ResetableAgent : Agent
    {
        abstract public void ResetRobot();
    }
}
