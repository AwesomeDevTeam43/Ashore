using UnityEngine;
using UnityEngine.UI;

public class ArenaScoreUIBootstrap : MonoBehaviour
{
    public ArenaScoreUI scoreUI;
    public GeneticTrainingArena arena;

    void Start()
    {
        if (arena == null)
            arena = FindObjectOfType<GeneticTrainingArena>();
        if (scoreUI == null)
            scoreUI = FindObjectOfType<ArenaScoreUI>();
    }

    void Update()
    {
        if (arena != null && scoreUI != null)
        {
            scoreUI.SetScore(arena.PlayerWins, arena.EnemyWins);
        }
    }
}
