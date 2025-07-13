using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Godot;
using Newtonsoft.Json;

namespace FourColors
{

    public static class LoggerManager
    {
        private static readonly string logPath = "user://app.log";

        public static void Info(string message) => Log("INFO", message);
        public static void Warn(string message) => Log("WARN", message);
        public static void Error(string message) => Log("ERROR", message);
        public static void Debug(string message) => Log("DEBUG", message);

        private static void Log(string level, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string finalMessage = $"{timestamp} | {level} | {message}\n";

            GD.Print(finalMessage);  // Print to Godot console

            try
            {
                using var file = Godot.FileAccess.Open(logPath, Godot.FileAccess.ModeFlags.WriteRead);
                file.SeekEnd(); // Move to end of file to append
                file.StoreString(finalMessage);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to write log: {ex.Message}");
            }
        }

    }

    public class GameLogic
    {
        enum Tile
        {// Horse,Queen,Rook,Cannon,King,Pawn,Bishop
            C1, C2, C3, C4, C7, C6, C5,
            C1_Green, C2_Green, C3_Green, C4_Green, C7_Green, C6_Green, C5_Green,
            C1_Red, C2_Red, C3_Red, C4_Red, C7_Red, C6_Red, C5_Red,
            C1_Yellow, C2_Yellow, C3_Yellow, C4_Yellow, C7_Yellow, C6_Yellow, C5_Yellow
        }
        // Honors
        /*
        C6, C6.Green, C6.Yellow, C6_Red  (Any 3)     P
        C1, C3, C4 (Same Color of this sets)         HRC
        C2, C7, C5 (Same Color of this sets)         KQB
        */
        // Sets
        /*
        Same Colors and same sets (except C7 (kings)
        */
        public static List<string> Deck { get; set; }
        public static List<string> Table { get; set; }
        public Dictionary<int, List<string>> playersHands { get; set; }
        public List<List<string>> Hands { get; set; }
        public int AITurn { get; set; }
        public string AItile { get; set; }

        public static string TileName(string tilevalue)
        {
            switch (tilevalue)
            {
                case "C1":
                    return "Horse White";
                case "C1_Green":
                    return "Horse Green";
                case "C1_Red":
                    return "Horse Red";
                case "C1_Yellow":
                    return "Horse Yellow";
                case "C2":
                    return "Queen White";
                case "C2_Green":
                    return "Queen Green";
                case "C2_Red":
                    return "Queen Red";
                case "C2_Yellow":
                    return "Queen Yellow";
                case "C3":
                    return "Rook White";
                case "C3_Green":
                    return "Rook Green";
                case "C3_Red":
                    return "Rook Red";
                case "C3_Yellow":
                    return "Rook Yellow";
                case "C4":
                    return "Cannon White";
                case "C4_Green":
                    return "Cannon Green";
                case "C4_Red":
                    return "Cannon Red";
                case "C4_Yellow":
                    return "Cannon Yellow";
                case "C5":
                    return "Bishop White";
                case "C5_Green":
                    return "Bishop Green";
                case "C5_Red":
                    return "Bishop Red";
                case "C5_Yellow":
                    return "Bishop Yellow";
                case "C6":
                    return "Pawn White";
                case "C6_Green":
                    return "Pawn Green";
                case "C6_Red":
                    return "Pawn Red";
                case "C6_Yellow":
                    return "Pawn Yellow";
                case "C7":
                    return "King White";
                case "C7_Green":
                    return "King Green";
                case "C7_Red":
                    return "King Red";
                case "C7_Yellow":
                    return "King Yellow";
            }

            return "Unknown";
        }

        private static List<string> alltile = new List<string>()
        {// Horse,Queen,Rook,Cannon,King,Pawn,Bishop
            "C1", "C2", "C3", "C4", "C6", "C5", "C7",
            "C1_Green", "C2_Green", "C3_Green", "C4_Green", "C6_Green", "C5_Green", "C7_Green",
            "C1_Red", "C2_Red", "C3_Red", "C4_Red", "C6_Red", "C5_Red", "C7_Red",
            "C1_Yellow", "C2_Yellow", "C3_Yellow", "C4_Yellow", "C6_Yellow", "C5_Yellow", "C7_Yellow"
        };


        ////////////////// Game Winning Conditions
        public static string WinCondition(List<string> v)
        {
            if (v.Count != 16)
                return "X";

            var focustile = v.ToList();
            List<List<string>> H = GetHonourSets(focustile); // Default Honours
            List<List<string>> CH = GetCombinations(H); // All Possible Honours Sets

            if (CH.Count == 0)
            {
                return "No Honour";
            }

            foreach (var h in CH)
            {
                focustile = v.ToList();
                //Console.WriteLine("Combi : " + String.Join(",", h));
                foreach (var item in h)
                {
                    focustile.Remove(item);
                }

                if (CheckColorSets(focustile).remainer == 0)
                {
                    return "WIN";
                }
            }

            return "X";
        }

        public static int CheckScore(List<string> v)
        {
            if (v.Contains("C7") || v.Contains("C7_Red") || v.Contains("C7_Green") || v.Contains("C7_Yellow"))
            {
                return 100;
            }
            else
            {
                return 150;
            }
        }

        public static (List<string> remainerlist, int remainer) CheckColorSets(List<string> remainer)
        {
            //Remove Kings
            remainer.RemoveAll(item => item.Contains("C7"));

            //Console.WriteLine("REMAINER : " + String.Join(",", remainer));
            //Check C1
            List<string> C1List = new List<string> { "C1", "C1_Green", "C1_Red", "C1_Yellow" };
            if (RemovesetsItems(ref remainer, C1List)) { return CheckColorSets(remainer); }
            //Check C2
            List<string> C2List = new List<string> { "C2", "C2_Green", "C2_Red", "C2_Yellow" };
            if (RemovesetsItems(ref remainer, C2List)) { return CheckColorSets(remainer); }
            //Check C3
            List<string> C3List = new List<string> { "C3", "C3_Green", "C3_Red", "C3_Yellow" };
            if (RemovesetsItems(ref remainer, C3List)) { return CheckColorSets(remainer); }
            //Check C4
            List<string> C4List = new List<string> { "C4", "C4_Green", "C4_Red", "C4_Yellow" };
            if (RemovesetsItems(ref remainer, C4List)) { return CheckColorSets(remainer); }
            //Check C6
            List<string> C6List = new List<string> { "C6", "C6_Green", "C6_Red", "C6_Yellow" };
            if (RemovesetsItems(ref remainer, C6List)) { return CheckColorSets(remainer); }
            //Check C5
            List<string> C5List = new List<string> { "C5", "C5_Green", "C5_Red", "C5_Yellow" };
            if (RemovesetsItems(ref remainer, C5List)) { return CheckColorSets(remainer); }


            return (remainer, remainer.Count);
        }

        static bool RemovesetsItems(ref List<string> v, List<string> presetList)
        {
            // Count occurrences of each matching item in v
            var matchCounts = v.GroupBy(x => x)
                               .Where(g => presetList.Contains(g.Key))
                               .ToDictionary(g => g.Key, g => g.Count());

            if (matchCounts.Count >= 3)
            {
                foreach (var key in matchCounts.Keys.ToList())
                {
                    v.Remove(key);
                }
                return true;
            }
            else
            {
                return false;
            }

        }
        // Honor sets
        private static List<List<string>> GetHonourSets(List<string> tile)
        {
            List<List<string>> honourSets = new List<List<string>>();
            List<List<string>> possibleHonours = new List<List<string>>()
        {
            new List<string> { "C2", "C7", "C5" },  // KQB
			new List<string> { "C2_Green", "C7_Green", "C5_Green" },
            new List<string> { "C2_Yellow", "C7_Yellow", "C5_Yellow" },
            new List<string> { "C2_Red", "C7_Red", "C5_Red" },

            new List<string> { "C1", "C3", "C4" },  // HRC
			new List<string> { "C1_Green", "C3_Green", "C4_Green" },
            new List<string> { "C1_Yellow", "C3_Yellow", "C4_Yellow" },
            new List<string> { "C1_Red", "C3_Red", "C4_Red" },

            new List<string> { "C6", "C6_Green", "C6_Red", "C6_Yellow" },  // All C6
			new List<string> { "C6", "C6_Green", "C6_Red" },
            new List<string> { "C6", "C6_Green", "C6_Yellow" },
            new List<string> { "C6", "C6_Red", "C6_Yellow" },
            new List<string> { "C6_Green", "C6_Red", "C6_Yellow" },
        };

            List<string> TrackTiles = new List<string>(tile);

            foreach (var honour in possibleHonours)
            {
                while (honour.All(TrackTiles.Contains))
                {
                    honourSets.Add(new List<string>(honour));

                    foreach (var item in honour)
                    {
                        TrackTiles.Remove(item);
                    }
                }
            }

            return honourSets;
        }

        public static bool CheckCastle(List<string> lists)
        {
            var alltilelist = alltile.ToList();
            if (lists.Count == 15)
            {
                foreach (var ct in alltile)
                {
                    var t = lists.ToList();
                    t.Add(ct);
                    if (WinCondition(t) == "WIN")
                    {
                        Console.WriteLine($"Win with {ct}");
                        return true;
                    }

                }
            }
            else if (lists.Count == 16)
            {
                if (WinCondition(lists) == "WIN")
                    return true;
            }

            return false;
        }

        private static List<List<string>> GetCombinations(List<List<string>> lists)
        {
            List<List<string>> result = new List<List<string>>();

            for (int i = 1; i <= lists.Count; i++)
            {
                var subsets = GetSubsets(lists, i);
                result.AddRange(subsets);
            }

            return result;
        }

        private static List<List<string>> GetSubsets(List<List<string>> lists, int subsetSize)
        {
            var result = new List<List<string>>();
            var indices = new int[subsetSize];

            for (int i = 0; i < subsetSize; i++)
            {
                indices[i] = i;
            }

            while (indices[0] <= lists.Count - subsetSize)
            {
                var subset = new List<string>();
                foreach (var index in indices)
                {
                    subset.AddRange(lists[index]);
                }
                result.Add(subset);

                int i = subsetSize - 1;
                while (i >= 0 && indices[i] == lists.Count - subsetSize + i)
                {
                    i--;
                }
                if (i >= 0)
                {
                    indices[i]++;
                    for (int j = i + 1; j < subsetSize; j++)
                    {
                        indices[j] = indices[j - 1] + 1;
                    }
                }
                else
                {
                    break;
                }
            }

            return result;
        }
        ///////////////////////////////////////////////////////////////////////////////////

        /////// Max logic 
        public class GameState
        {
            public List<string> Hand;
            public int point = 0;
        }

        public static double EvaluateState(GameState state)
        {
            if (WinCondition(state.Hand) == "WIN" && state.Hand.Count == 16)
                return 10000;

            double bestpoint = double.NegativeInfinity;

            var focustile = state.Hand.ToList();
            List<List<string>> H = GetHonourSets(focustile);
            List<List<string>> CH = GetCombinations(H);

            if (CH.Count == 0)
            {
                // Fallback scoring based only on color sets
                var rl = CheckColorSets(focustile).remainerlist;
                var r = CheckColorSets(focustile).remainer;

                double honorsgroup = 0;
                double partialBonus = CountPartialColorSets(rl) * 2;
                double colorPenalty = MaxPair(rl) * 10;
                double duplicatepenalty = 0;
                if (r > 0)
                {
                    foreach (var item in rl.Distinct())
                    {
                        int count = state.Hand.Count(x => x == item);
                        if (count > duplicatepenalty)
                            duplicatepenalty = count;
                    }
                }

                bestpoint = honorsgroup + partialBonus - colorPenalty - duplicatepenalty;
                return bestpoint;
            }

            foreach (var h in CH)
            {
                focustile = state.Hand.ToList();
                foreach (var item in h)
                    focustile.Remove(item);

                var rl = CheckColorSets(focustile).remainerlist;
                var r = CheckColorSets(focustile).remainer;

                double honorsgroup = h.Count(); // HonorSets forms
                double partialBonus = CountPartialColorSets(rl) * 2; // If form colorset
                double colorPenalty = MaxPair(rl) * 10; // Remainer that doest for anything 
                double duplicatepenalty = 0;
                if (r > 0)
                {
                    foreach (var item in rl.Distinct())
                    {
                        int count = state.Hand.Count(x => x == item);
                        if (count > duplicatepenalty)
                            duplicatepenalty = count;
                    }
                }

                int maxMatch = 0;

                var prefixGroups = rl.GroupBy(s => s.Substring(0, 2))
                                       .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var prefix in prefixGroups.Keys)
                {
                    int handCount = prefixGroups[prefix].Count;
                    int tableCount = Table.Where(t =>
                        t.StartsWith(prefix) && !rl.Contains(t)
                    ).Count();

                    maxMatch = Math.Max(maxMatch, Math.Min(handCount, tableCount));
                }


                double point = honorsgroup + partialBonus - colorPenalty - duplicatepenalty - maxMatch;
                bestpoint = Math.Max(bestpoint, point);

                Console.WriteLine($"Combi of {honorsgroup}*1 + {CountPartialColorSets(rl)}*2 - {MaxPair(rl)}*10 - {duplicatepenalty} - {maxMatch}  = {point} : honors={String.Join(",", h)} colors=={String.Join(",", rl)}  Best point = {bestpoint}");
            }

            return bestpoint;
        }


        public static double MaxPair(List<string> list)
        {
            if (list.Count == 0)
                return 0;

            var grouped = list
            .Distinct() // Remove duplicates
            .GroupBy(card => card.Substring(0, 2)) // Group by first 2 chars
            .Select(g => new { Prefix = g.Key, Count = g.Count() })
            .ToList();

            var max = grouped.Max(g => g.Count);
            return list.Count - max;
        }

        public static int CountPartialColorSets(List<string> remaining)
        {
            var colorGroups = remaining
                .GroupBy(card => card.Substring(0, 2))
                .Where(g => g.Count() == 2 || g.Count() == 3);

            return colorGroups.Count();
        }


        public static string MAX_AI_DISCARD(GameState state)
        {
            if (WinCondition(state.Hand) == "WIN" && state.Hand.Count == 16)
                return "WIN";

            double handpoint = double.NegativeInfinity;
            string tiletodiscard = "";
            foreach (var tile in state.Hand)
            {
                if (tile.Contains("C7"))
                    continue;

                var focustile = state.Hand.ToList();
                focustile.Remove(tile);
                double point = EvaluateState(new GameState() { Hand = focustile });
                Console.WriteLine($"discard {tile} point {point}");
                if (point > handpoint)
                {
                    tiletodiscard = tile;
                    handpoint = point;
                }
            }
            return tiletodiscard;
        }

        private static readonly string saveFilePath = ProjectSettings.GlobalizePath("user://save_data.json");

        public static void Savepoint(int points)
        {
            try
            {
                var data = new Dictionary<string, int> { { "points", points } };

                // Make sure directory exists
                var directory = Path.GetDirectoryName(saveFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(saveFilePath, json); // using System.IO
                //GD.Print("Saved points to: " + saveFilePath);
            }
            catch (Exception e)
            {
                GD.PrintErr("Save failed: " + e.Message);
            }
        }

        public static int Loadpoint()
        {
            try
            {
                if (!File.Exists(saveFilePath))
                {
                    GD.Print("Save file not found.");
                    return 0;
                }

                var json = File.ReadAllText(saveFilePath);
                var data = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);

                if (data != null && data.ContainsKey("points"))
                {
                    GD.Print("Loaded points: " + data["points"]);
                    return data["points"];
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("Load failed: " + e.Message);
            }

            return 0;
        }

    }
}