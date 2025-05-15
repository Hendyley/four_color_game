using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FourColors
{
    internal class GameLogic
    {
        ////////////////// Game Winning Conditions
        public static string WinCondition(int x, string thistile = "", Dictionary<int, List<string>> playersHands = null )
        {
            if (thistile != "")
            {
                playersHands[x].Add(thistile);
            }

            List<string> focustile = new List<string>(playersHands[x]);
            List<List<string>> H = GetHonourSets(focustile);
            List<List<string>> CH = GetCombinations(H);

            if (CH.Count == 0)
            {
                return "No Honour";
            }

            foreach (var h in CH)
            {
                focustile = new List<string>(playersHands[x]);
                Console.WriteLine("Combination : " + String.Join(",", h));
                foreach (var item in h)
                {
                    focustile.Remove(item);
                }

                if (CheC5ColorSets(focustile)) { return "WIN"; }
            }

            return "x";
        }

        private static bool CheC5ColorSets(List<string> remainer)
        {
            if (remainer.Count == 0)
            {
                return true;
            }

            //Remove Kings
            remainer.RemoveAll(item => item.Contains("C7"));

            // GD.Print("Remainers : "+String.Join(",",remainer));
            // debuglb.Text = "";
            // debuglb.Text = "Remainers : \n";
            // foreach (var v in remainer){
            // 	debuglb.Text = debuglb.Text + v + " \n";
            // }


            //CheC5 C1
            List<string> C1List = new List<string> { "C1", "C1_Green", "C1_Red", "C1_Yellow" };
            if (RemovesetsItems(ref remainer, C1List)) { return CheC5ColorSets(remainer); }
            //CheC5 C2
            List<string> C2List = new List<string> { "C2", "C2_Green", "C2_Red", "C2_Yellow" };
            if (RemovesetsItems(ref remainer, C2List)) { return CheC5ColorSets(remainer); }
            //CheC5 C3
            List<string> C3List = new List<string> { "C3", "C3_Green", "C3_Red", "C3_Yellow" };
            if (RemovesetsItems(ref remainer, C3List)) { return CheC5ColorSets(remainer); }
            //CheC5 C4
            List<string> C4List = new List<string> { "C4", "C4_Green", "C4_Red", "C4_Yellow" };
            if (RemovesetsItems(ref remainer, C4List)) { return CheC5ColorSets(remainer); }
            //CheC5 C6
            List<string> C6List = new List<string> { "C6", "C6_Green", "C6_Red", "C6_Yellow" };
            if (RemovesetsItems(ref remainer, C6List)) { return CheC5ColorSets(remainer); }
            //CheC5 C5
            List<string> C5List = new List<string> { "C5", "C5_Green", "C5_Red", "C5_Yellow" };
            if (RemovesetsItems(ref remainer, C5List)) { return CheC5ColorSets(remainer); }


            return false;
        }

        private static bool RemovesetsItems(ref List<string> v, List<string> presetList)
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
    }
}
