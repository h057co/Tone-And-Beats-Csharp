using System;

namespace AudioAnalyzer.Services;

public interface IKeyDisplayService
{
    bool[] CalculateScaleNotes(int keyIndex, string modeText, bool showRelativeKey);
    int GetCurrentTonicIndex(int keyIndex, string modeText, bool showRelativeKey);
    string GetKeyDisplayText(string keyText, string modeText, int keyIndex, bool showRelativeKey);
}

public class KeyDisplayService : IKeyDisplayService
{
    private static readonly string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public bool[] CalculateScaleNotes(int keyIndex, string modeText, bool showRelativeKey)
    {
        var notes = new bool[12];
        if (keyIndex < 0 || string.IsNullOrEmpty(modeText))
            return notes;

        string currentMode = modeText;
        int currentRoot = keyIndex;

        if (showRelativeKey)
        {
            if (modeText == "Major")
            {
                currentMode = "Minor";
                currentRoot = (keyIndex + 9) % 12;
            }
            else
            {
                currentMode = "Major";
                currentRoot = (keyIndex + 3) % 12;
            }
        }

        int[] pattern = currentMode == "Major" 
            ? new[] { 0, 2, 4, 5, 7, 9, 11 } 
            : new[] { 0, 2, 3, 5, 7, 8, 10 };

        foreach (int interval in pattern)
        {
            notes[(currentRoot + interval) % 12] = true;
        }

        return notes;
    }

    public int GetCurrentTonicIndex(int keyIndex, string modeText, bool showRelativeKey)
    {
        if (keyIndex < 0) return -1;
        if (!showRelativeKey) return keyIndex;

        if (modeText == "Major")
            return (keyIndex - 3 + 12) % 12;
        else
            return (keyIndex + 3) % 12;
    }

    public string GetKeyDisplayText(string keyText, string modeText, int keyIndex, bool showRelativeKey)
    {
        if (string.IsNullOrEmpty(keyText) || keyText == "--")
            return "--";
            
        if (showRelativeKey)
        {
            if (keyIndex < 0 || string.IsNullOrEmpty(modeText))
                return "--";

            int relativeIndex;
            string relativeMode;

            if (modeText == "Major")
            {
                relativeIndex = (keyIndex - 3 + 12) % 12;
                relativeMode = "Minor";
            }
            else
            {
                relativeIndex = (keyIndex + 3) % 12;
                relativeMode = "Major";
            }

            return $"{NoteNames[relativeIndex]} {relativeMode}";
        }
        
        return $"{keyText} {modeText}";
    }
}
