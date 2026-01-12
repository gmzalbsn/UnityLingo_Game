using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WordListWrapper
{
    public List<string> fourLetters;
    public List<string> fiveLetters;
    public List<string> sixLetters;
}

public class WordBank : MonoBehaviour
{
    [SerializeField] private bool logOnLoad = false;

    private WordListWrapper words;

    private void Awake()
    {
        LoadWordsFromJson();
    }

    private void LoadWordsFromJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("words");
        if (jsonFile == null)
        {
            Debug.LogError("Missing words.json. Place it at Assets/Resources/words.json");
            return;
        }

        words = JsonUtility.FromJson<WordListWrapper>(jsonFile.text);
        if (words == null)
        {
            Debug.LogError("Failed to parse words.json.");
            return;
        }

        if (logOnLoad) Debug.Log("[WordBank] Loaded words.");
    }

    public string GetRandomWordByLength(int length)
    {
        if (words == null) return "TEST";

        List<string> list = null;

        switch (length)
        {
            case 4: list = words.fourLetters; break;
            case 5: list = words.fiveLetters; break;
            case 6: list = words.sixLetters; break;
            default: list = words.fiveLetters; break;
        }

        if (list == null || list.Count == 0) return "TEST";

        int index = Random.Range(0, list.Count);
        return list[index].ToUpperInvariant();
    }
}