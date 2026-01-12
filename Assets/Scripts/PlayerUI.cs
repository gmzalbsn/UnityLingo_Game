using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private LingoGameManager gameManager;

    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text correctWordText;
    [SerializeField] private Button readyButton;

    private string player1Name = "P1";
    private string player2Name = "P2";

    private void Awake()
    {
        if (gameManager == null ||
            roundText == null ||
            timerText == null ||
            scoreText == null ||
            correctWordText == null ||
            readyButton == null)
        {
            Debug.LogError("[PlayerUI] Missing references.", this);
            enabled = false;
            return;
        }

        readyButton.onClick.AddListener(OnReadyClicked);
    }

    public void SetPlayerNames(string p1, string p2)
    {
        player1Name = string.IsNullOrEmpty(p1) ? "P1" : p1;
        player2Name = string.IsNullOrEmpty(p2) ? "P2" : p2;
    }

    public void UpdateRound(int roundNumber, int wordLength, int starterPlayer)
    {
        string starter = (starterPlayer == 1) ? player1Name : player2Name;
        string steal = (starterPlayer == 1) ? player2Name : player1Name;

        roundText.text =
            $"Round: {roundNumber}\n" +
            $"Word: {wordLength} letters | Starter: {starter} | Steal: {steal}\n" +
            "Press READY to start.";

        correctWordText.text = string.Empty;
        timerText.text = string.Empty;
    }

    public void ShowReadyPhase()
    {
        readyButton.gameObject.SetActive(true);
        readyButton.interactable = true;
    }

    public void ShowMainPhase(int currentPlayer, int attemptNumber, float remainingSeconds)
    {
        readyButton.gameObject.SetActive(false);
        UpdateMainTimer(currentPlayer, attemptNumber, remainingSeconds);
    }

    public void ShowStealPhase(int stealPlayer, float remainingSeconds)
    {
        readyButton.gameObject.SetActive(false);
        UpdateStealTimer(stealPlayer, remainingSeconds);
    }

    public void UpdateMainTimer(int currentPlayer, int attemptNumber, float remainingSeconds)
    {
        string name = (currentPlayer == 1) ? player1Name : player2Name;
        int seconds = Mathf.CeilToInt(remainingSeconds);
        timerText.text = $"{name} | Attempt {attemptNumber} | Time: {seconds}s";
    }

    public void UpdateStealTimer(int currentPlayer, float remainingSeconds)
    {
        string name = (currentPlayer == 1) ? player1Name : player2Name;
        int seconds = Mathf.CeilToInt(remainingSeconds);
        timerText.text = $"STEAL | {name} | Time: {seconds}s";
    }

    public void UpdateScores(int score1, int score2)
    {
        scoreText.text = $"{player1Name}: {score1}  |  {player2Name}: {score2}";
    }

    public void ShowCorrectWord(string targetWord)
    {
        correctWordText.text = $"Correct Word: {targetWord}";
    }

    public void ShowGameEnd(int score1, int score2)
    {
        roundText.text =
            $"GAME OVER\n" +
            $"{player1Name}: {score1}  |  {player2Name}: {score2}\n" +
            "Restart the scene to play again.";
    }

    private void OnReadyClicked()
    {
        readyButton.interactable = false;
        gameManager.OnReadyPressed();
    }
}
