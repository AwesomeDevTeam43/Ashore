namespace Ashore.AI.Pathfinding
{
    public interface IAgentPathfinder
    {
        System.Collections.Generic.List<UnityEngine.Vector2> FindPath(UnityEngine.Vector2 start, UnityEngine.Vector2 target);
    }
}
