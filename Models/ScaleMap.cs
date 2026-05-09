using System;

namespace AudioAnalyzer.Models
{
    public class ScaleMap
    {
        public int RootNote { get; set; } // 0-11 (C to B)
        public string Mode { get; set; } = "Major";
        public bool[] Notes { get; set; } = new bool[12];

        public static ScaleMap FromKey(string key)
        {
            var map = new ScaleMap();
            if (string.IsNullOrEmpty(key)) return map;

            // Clean the key string (e.g., "Cmaj" -> "C", "Am" -> "A")
            string root = key.Replace("maj", "").Replace("min", "").Replace("m", "");
            map.Mode = key.ToLower().Contains("min") || key.EndsWith("m") ? "Minor" : "Major";

            string[] notes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            map.RootNote = Array.IndexOf(notes, root.ToUpper());
            if (map.RootNote == -1) return map;

            // Intervals: Major (2, 2, 1, 2, 2, 2, 1), Minor (2, 1, 2, 2, 1, 2, 2)
            int[] intervals = map.Mode == "Major" 
                ? new[] { 0, 2, 4, 5, 7, 9, 11 } 
                : new[] { 0, 2, 3, 5, 7, 8, 10 };

            foreach (int interval in intervals)
            {
                map.Notes[(map.RootNote + interval) % 12] = true;
            }
            
            return map;
        }
    }
}
