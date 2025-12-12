using UnityEngine;
using UnityEngine.UI;

public class ArenaScoreUI : MonoBehaviour
{
    public Text scoreText;
    public Text bestGenomeText;

    // Call this to update the score display
    public void SetScore(int playerScore, int enemyScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Player: {playerScore}  |  Enemy: {enemyScore}";
        }
    }

    // Call this to display the best genome
    public void SetBestGenome(string genomeInfo)
    {
        if (bestGenomeText != null)
        {
            bestGenomeText.text = genomeInfo;
        }
    }
}
