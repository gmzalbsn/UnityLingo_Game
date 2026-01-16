using UnityEngine;

public class LingoInputController : MonoBehaviour
{
    [SerializeField] private LingoGameManager gameManager;
    [SerializeField] private LingoBoardUI boardUI;
    [SerializeField] private bool allowEnterToSkipAttempt = false;
    private void Awake()
    {
        if (gameManager == null || boardUI == null)
        {
            Debug.LogError("[LingoInputController] Missing references (gameManager/boardUI).", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (gameManager == null || boardUI == null) return;

        if (gameManager.IsWaitingForReady)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                gameManager.OnReadyPressed();
            }
            return;
        }

        if (!gameManager.CanType) return;
        if (!boardUI.CanAcceptInput) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (boardUI.IsCurrentRowFull())
            {
                string guess = boardUI.GetCurrentGuess();
                gameManager.SubmitGuessFromUI(guess);
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            boardUI.RemoveLetter();
            return;
        }

        string inputString = Input.inputString;

        foreach (char c in inputString)
        {
            if (char.IsLetter(c))
            {
                boardUI.AddLetter(char.ToUpperInvariant(c));
            }
        }
    }
}