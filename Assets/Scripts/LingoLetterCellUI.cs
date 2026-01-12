using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LingoLetterCellUI : MonoBehaviour
{
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Image backgroundImage;

    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color presentColor = Color.yellow;
    [SerializeField] private Color absentColor = Color.gray;
    [SerializeField] private Color stealRowDefaultColor = new Color(1f, 0.9f, 0.6f);

    private bool isStealRow;

    private void Awake()
    {
        if (letterText == null || backgroundImage == null)
        {
            enabled = false;
            return;
        }

        SetStealRow(false);
        SetLetter('\0');
    }

    public void SetLetter(char c)
    {
        letterText.text = (c == '\0') ? string.Empty : c.ToString();
    }

    public char GetLetterChar()
    {
        string t = letterText.text;
        if (string.IsNullOrEmpty(t)) return '\0';
        return t[0];
    }

    public void SetFeedback(LingoGameManager.LetterFeedback feedback)
    {
        switch (feedback)
        {
            case LingoGameManager.LetterFeedback.Correct:
                backgroundImage.color = correctColor;
                break;
            case LingoGameManager.LetterFeedback.Present:
                backgroundImage.color = presentColor;
                break;
            case LingoGameManager.LetterFeedback.Absent:
                backgroundImage.color = absentColor;
                break;
            default:
                ApplyDefaultColor();
                break;
        }
    }

    public void SetStealRow(bool value)
    {
        isStealRow = value;
        ApplyDefaultColor();
    }

    private void ApplyDefaultColor()
    {
        backgroundImage.color = isStealRow ? stealRowDefaultColor : defaultColor;
    }
}
