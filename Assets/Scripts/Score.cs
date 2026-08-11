using UnityEngine;
using TMPro;
using YG;
public class Score : MonoBehaviour
{
    [SerializeField] TMP_Text scoreText;
    private int currentScore;
    private int bestScore;

    private void Start()
    {
        bestScore = YG2.saves.bestRunScore;
    }

    public void ReturnScore(int score)
    {
        currentScore = score;
        scoreText.text = currentScore.ToString();
        isBestScore();
    }
    public int GetScore()
    {
        return currentScore;
    }
    void isBestScore()
    {
        if (bestScore < currentScore)
        {
            bestScore = currentScore;
            YG2.saves.bestRunScore = bestScore;
            Leaderboard(bestScore);
            YG2.SaveProgress();
        }
    }

    public void Leaderboard(int score)
    {
        YG2.SetLeaderboard("Leaderboard", score);
    }
}
