using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LingoGameManager : MonoBehaviour
{
    private enum GameState
    {
        WaitingForReady,
        WaitingForPlayerGuess,
        WaitingForStealGuess,
        RoundEnded,
        GameEnded
    }

    public enum LetterFeedback
    {
        None,
        Correct,
        Present,
        Absent
    }

    public struct LetterResult
    {
        public char Letter;
        public LetterFeedback Feedback;

        public LetterResult(char letter, LetterFeedback feedback)
        {
            Letter = letter;
            Feedback = feedback;
        }
    }

    [SerializeField] private int maxAttemptsPerRound = 5;
    [SerializeField] private float endRoundDelaySeconds = 1f;
    [SerializeField] private float rowTimeLimitSeconds = 30f;
    [SerializeField] private float stealTimeLimitSeconds = 15f;
    [SerializeField] private float stealWrongPenaltySeconds = 5f;

    [SerializeField] private List<int> roundWordLengths = new List<int> { 4, 5, 5, 5, 6 };

    [SerializeField] private WordBank wordBank;
    [SerializeField] private LingoBoardUI boardUI;
    [SerializeField] private PlayerUI playerUI;

    private GameState state;

    private int currentRoundIndex;
    private int currentStartingPlayer;
    private int currentPlayer;

    private int player1Score;
    private int player2Score;

    private string targetWord;
    private int attemptsUsed;

    private float rowTimeRemaining;
    private float stealTimeRemaining;

    private int stealRowIndex = -1;

    public bool CanType => state == GameState.WaitingForPlayerGuess || state == GameState.WaitingForStealGuess;
    public bool IsWaitingForReady => state == GameState.WaitingForReady;
    public int CurrentWordLength => string.IsNullOrEmpty(targetWord) ? 0 : targetWord.Length;

    private void Awake()
    {
        if (wordBank == null || boardUI == null || playerUI == null)
        {
            Debug.LogError("[LingoGameManager] Missing references (wordBank/boardUI/playerUI).", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (!enabled) return;

        state = GameState.WaitingForReady;
        currentRoundIndex = 0;
        currentStartingPlayer = 1;
        currentPlayer = 1;

        StartNewRound();
    }

    private void Update()
    {
        if (state == GameState.WaitingForPlayerGuess)
        {
            rowTimeRemaining -= Time.deltaTime;

            int attemptNumber = attemptsUsed + 1;
            playerUI.UpdateMainTimer(currentPlayer, attemptNumber, rowTimeRemaining);

            if (rowTimeRemaining <= 0f)
            {
                HandleMainPhaseTimeout();
            }
        }
        else if (state == GameState.WaitingForStealGuess)
        {
            stealTimeRemaining -= Time.deltaTime;

            playerUI.UpdateStealTimer(currentPlayer, stealTimeRemaining);

            if (stealTimeRemaining <= 0f)
            {
                HandleStealPhaseTimeout();
            }
        }
    }

    private void StartNewRound()
    {
        if (currentRoundIndex >= roundWordLengths.Count)
        {
            EndGame();
            return;
        }

        state = GameState.WaitingForReady;

        int wordLength = Mathf.Max(1, roundWordLengths[currentRoundIndex]);
        targetWord = wordBank.GetRandomWordByLength(wordLength);

        attemptsUsed = 0;
        stealRowIndex = -1;

        char firstLetter = targetWord[0];

        boardUI.SetupBoard(wordLength, maxAttemptsPerRound, firstLetter);
        boardUI.LockInput();

        playerUI.UpdateRound(currentRoundIndex + 1, wordLength, currentStartingPlayer);
        playerUI.UpdateScores(player1Score, player2Score);
        playerUI.ShowReadyPhase();
    }

    public void OnReadyPressed()
    {
        if (state != GameState.WaitingForReady) return;

        state = GameState.WaitingForPlayerGuess;

        currentPlayer = currentStartingPlayer;
        rowTimeRemaining = rowTimeLimitSeconds;

        boardUI.SetActiveAttemptRow(0);
        boardUI.UnlockInput();

        int attemptNumber = attemptsUsed + 1;
        playerUI.ShowMainPhase(currentPlayer, attemptNumber, rowTimeRemaining);
    }

    public void SubmitGuessFromUI(string guess)
    {
        if (!CanType) return;
        if (string.IsNullOrEmpty(guess)) return;

        guess = guess.ToUpperInvariant();

        if (guess.Length != targetWord.Length) return;

        if (state == GameState.WaitingForPlayerGuess)
        {
            ProcessMainGuess(guess);
        }
        else if (state == GameState.WaitingForStealGuess)
        {
            ProcessStealGuess(guess);
        }
    }

    public void SkipAttemptFromUI()
    {
        if (state != GameState.WaitingForPlayerGuess) return;

        attemptsUsed++;

        if (attemptsUsed >= maxAttemptsPerRound)
        {
            StartStealPhase();
            return;
        }

        rowTimeRemaining = rowTimeLimitSeconds;

        boardUI.SetActiveAttemptRow(attemptsUsed);
        boardUI.UnlockInput();

        int attemptNumber = attemptsUsed + 1;
        playerUI.ShowMainPhase(currentPlayer, attemptNumber, rowTimeRemaining);
    }

    private void ProcessMainGuess(string guess)
    {
        var results = EvaluateGuess(targetWord, guess);
        bool isCorrect = IsAllCorrect(results);

        boardUI.ShowGuessResult(attemptsUsed, results);

        if (isCorrect)
        {
            int scoreGained = CalculateScore(attemptsUsed, true);
            AddScore(currentPlayer, scoreGained);

            playerUI.ShowCorrectWord(targetWord);
            playerUI.UpdateScores(player1Score, player2Score);

            EndRound();
            return;
        }

        attemptsUsed++;

        if (attemptsUsed >= maxAttemptsPerRound)
        {
            StartStealPhase();
            return;
        }

        rowTimeRemaining = rowTimeLimitSeconds;

        boardUI.SetActiveAttemptRow(attemptsUsed);
        boardUI.UnlockInput();

        int attemptNumber = attemptsUsed + 1;
        playerUI.ShowMainPhase(currentPlayer, attemptNumber, rowTimeRemaining);
    }

    private void StartStealPhase()
    {
        state = GameState.WaitingForStealGuess;

        int stealPlayer = (currentStartingPlayer == 1) ? 2 : 1;
        currentPlayer = stealPlayer;

        stealRowIndex = boardUI.CreateStealRow();
        stealTimeRemaining = stealTimeLimitSeconds;

        playerUI.ShowStealPhase(stealPlayer, stealTimeRemaining);

        boardUI.UnlockInput();
    }

    private void ProcessStealGuess(string guess)
    {
        int stealPlayer = (currentStartingPlayer == 1) ? 2 : 1;

        var results = EvaluateGuess(targetWord, guess);
        bool isCorrect = IsAllCorrect(results);

        if (stealRowIndex >= 0)
        {
            boardUI.ShowGuessResult(stealRowIndex, results);
        }

        if (isCorrect)
        {
            int scoreGained = CalculateScore(attemptsUsed, false);
            AddScore(stealPlayer, scoreGained);

            playerUI.ShowCorrectWord(targetWord);
            playerUI.UpdateScores(player1Score, player2Score);
        }
        EndRound();
    }


    private void HandleMainPhaseTimeout()
    {
        attemptsUsed++;

        if (attemptsUsed >= maxAttemptsPerRound)
        {
            StartStealPhase();
            return;
        }

        rowTimeRemaining = rowTimeLimitSeconds;

        boardUI.SetActiveAttemptRow(attemptsUsed);
        boardUI.UnlockInput();

        int attemptNumber = attemptsUsed + 1;
        playerUI.ShowMainPhase(currentPlayer, attemptNumber, rowTimeRemaining);
    }

    private void HandleStealPhaseTimeout()
    {
        EndRound();
    }

    private void EndRound()
    {
        state = GameState.RoundEnded;

        boardUI.LockInput();
        playerUI.ShowCorrectWord(targetWord);
        playerUI.UpdateScores(player1Score, player2Score);

        StartCoroutine(EndRoundRoutine());
    }

    private IEnumerator EndRoundRoutine()
    {
        if (endRoundDelaySeconds > 0f)
            yield return new WaitForSeconds(endRoundDelaySeconds);

        currentRoundIndex++;
        currentStartingPlayer = (currentStartingPlayer == 1) ? 2 : 1;
        currentPlayer = currentStartingPlayer;

        StartNewRound();
    }

    private void EndGame()
    {
        state = GameState.GameEnded;
        boardUI.LockInput();
        playerUI.ShowGameEnd(player1Score, player2Score);
    }

    private void AddScore(int player, int amount)
    {
        if (player == 1) player1Score += amount;
        else player2Score += amount;
    }

    private int CalculateScore(int usedAttempts, bool mainPlayerSolved)
    {
        int baseScore = 100;

        if (mainPlayerSolved)
        {
            int bonus = Mathf.Max(0, (maxAttemptsPerRound - usedAttempts) * 10);
            return baseScore + bonus;
        }

        return baseScore;
    }

    private static bool IsAllCorrect(List<LetterResult> results)
    {
        if (results == null || results.Count == 0) return false;

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].Feedback != LetterFeedback.Correct) return false;
        }

        return true;
    }

    private static List<LetterResult> EvaluateGuess(string target, string guess)
    {
        int len = target.Length;
        var results = new List<LetterResult>(len);

        var targetChars = target.ToCharArray();
        var guessChars = guess.ToCharArray();
        var used = new bool[len];

        for (int i = 0; i < len; i++)
        {
            if (guessChars[i] == targetChars[i])
            {
                results.Add(new LetterResult(guessChars[i], LetterFeedback.Correct));
                used[i] = true;
            }
            else
            {
                results.Add(new LetterResult(guessChars[i], LetterFeedback.None));
            }
        }

        for (int i = 0; i < len; i++)
        {
            if (results[i].Feedback == LetterFeedback.Correct) continue;

            bool found = false;

            for (int j = 0; j < len; j++)
            {
                if (used[j]) continue;
                if (guessChars[i] == targetChars[j])
                {
                    used[j] = true;
                    found = true;
                    break;
                }
            }

            results[i] = new LetterResult(guessChars[i], found ? LetterFeedback.Present : LetterFeedback.Absent);
        }

        return results;
    }
}
