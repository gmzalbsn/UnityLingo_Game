using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class LingoBoardUI : MonoBehaviour
{
    [SerializeField] private LingoLetterCellUI cellPrefab;
    [SerializeField] private Vector2 cellSize = new Vector2(80f, 80f);
    [SerializeField] private Vector2 cellSpacing = new Vector2(8f, 8f);
    [SerializeField] private RectOffset gridPadding;

    private GridLayoutGroup grid;

    private readonly List<List<LingoLetterCellUI>> rows = new List<List<LingoLetterCellUI>>();
    private int cols;

    private int currentRowIndex;
    private int currentLetterIndex;

    private bool inputLocked;
    private bool useFixedFirstLetter;
    private char fixedFirstLetter;

    public bool CanAcceptInput => !inputLocked && rows.Count > 0;

    private void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();

        if (cellPrefab == null)
        {
            Debug.LogError("[LingoBoardUI] Missing cellPrefab.", this);
            enabled = false;
            return;
        }

        if (gridPadding == null)
            gridPadding = new RectOffset(8, 8, 8, 8);

        ApplyGridSettings();
    }

    private void ApplyGridSettings()
    {
        grid.cellSize = cellSize;
        grid.spacing = cellSpacing;
        grid.padding = gridPadding;

        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.MiddleCenter;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
    }

    public void SetupBoard(int wordLength, int maxAttempts, char firstLetter)
    {
        ClearChildren();
        rows.Clear();

        cols = Mathf.Max(1, wordLength);

        useFixedFirstLetter = true;
        fixedFirstLetter = char.ToUpperInvariant(firstLetter);

        grid.constraintCount = cols;
        ApplyGridSettings();

        for (int i = 0; i < maxAttempts; i++)
        {
            CreateRow(isStealRow: false);
        }

        SetActiveAttemptRow(0);
        LockInput();
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private int CreateRow(bool isStealRow)
    {
        int newRowIndex = rows.Count;
        var rowCells = new List<LingoLetterCellUI>(cols);

        for (int col = 0; col < cols; col++)
        {
            var cell = Instantiate(cellPrefab, transform);
            cell.SetStealRow(isStealRow);
            cell.SetFeedback(LingoGameManager.LetterFeedback.None);

            if (col == 0 && useFixedFirstLetter)
                cell.SetLetter(fixedFirstLetter);
            else
                cell.SetLetter('\0');

            rowCells.Add(cell);
        }

        rows.Add(rowCells);
        return newRowIndex;
    }

    public void SetActiveAttemptRow(int attemptIndex)
    {
        if (attemptIndex < 0 || attemptIndex >= rows.Count) return;

        currentRowIndex = attemptIndex;
        currentLetterIndex = useFixedFirstLetter ? 1 : 0;

        ClearRowLetters(currentRowIndex);
    }

    private void ClearRowLetters(int rowIndex)
    {
        var rowCells = rows[rowIndex];

        for (int col = 0; col < cols; col++)
        {
            if (useFixedFirstLetter && col == 0)
            {
                rowCells[col].SetLetter(fixedFirstLetter);
                rowCells[col].SetFeedback(LingoGameManager.LetterFeedback.None);
                continue;
            }

            rowCells[col].SetLetter('\0');
            rowCells[col].SetFeedback(LingoGameManager.LetterFeedback.None);
        }
    }

    public int CreateStealRow()
    {
        int index = CreateRow(isStealRow: true);
        currentRowIndex = index;
        currentLetterIndex = useFixedFirstLetter ? 1 : 0;
        inputLocked = false;
        return index;
    }

    public void AddLetter(char c)
    {
        if (!CanAcceptInput) return;
        if (currentLetterIndex >= cols) return;

        if (useFixedFirstLetter && currentLetterIndex == 0) currentLetterIndex = 1;

        var cell = rows[currentRowIndex][currentLetterIndex];
        cell.SetLetter(c);
        cell.SetFeedback(LingoGameManager.LetterFeedback.None);

        currentLetterIndex++;
    }

    public void RemoveLetter()
    {
        if (!CanAcceptInput) return;

        int minIndex = useFixedFirstLetter ? 1 : 0;
        if (currentLetterIndex <= minIndex) return;

        currentLetterIndex--;

        var cell = rows[currentRowIndex][currentLetterIndex];
        cell.SetLetter('\0');
        cell.SetFeedback(LingoGameManager.LetterFeedback.None);
    }

    public bool IsCurrentRowFull()
    {
        return currentLetterIndex == cols;
    }

    public string GetCurrentGuess()
    {
        if (!CanAcceptInput) return string.Empty;

        var sb = new StringBuilder(cols);
        var rowCells = rows[currentRowIndex];

        for (int col = 0; col < cols; col++)
        {
            if (useFixedFirstLetter && col == 0)
            {
                sb.Append(fixedFirstLetter);
                continue;
            }

            char ch = rowCells[col].GetLetterChar();
            if (ch != '\0') sb.Append(ch);
        }

        return sb.ToString().ToUpperInvariant();
    }

    public void ShowGuessResult(int rowIndex, List<LingoGameManager.LetterResult> results)
    {
        if (results == null) return;
        if (rowIndex < 0 || rowIndex >= rows.Count) return;

        var rowCells = rows[rowIndex];
        int max = Mathf.Min(results.Count, cols);

        for (int col = 0; col < max; col++)
        {
            var r = results[col];
            rowCells[col].SetLetter(r.Letter);
            rowCells[col].SetFeedback(r.Feedback);
        }
    }

    public void LockInput() => inputLocked = true;
    public void UnlockInput() => inputLocked = false;
}
