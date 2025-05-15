using System;
using System.Collections.Generic;
using System.Linq;

namespace Program
{
    enum Tile
    {// Horse,Queen,Rook,Cannon,King,Pawn,Bishop
        C1, C2, C3, C4, C5, C6, C7,
        C1_Green, C2_Green, C3_Green, C4_Green, C5_Green, C6_Green, C7_Green,
        C1_Red, C2_Red, C3_Red, C4_Red, C5_Red, C6_Red, C7_Red,
        C1_Yellow, C2_Yellow, C3_Yellow, C4_Yellow, C5_Yellow, C6_Yellow, C7_Yellow
    }
    // Honors
    /*
    C6, C6.Green, C6.Yellow, C6_Red  (Any 3)     P
    C1, C3, C4 (Same Color of this sets)         HRC
    C2, C5, C7 (Same Color of this sets)         KQB
    */
    // Sets
    /*
    Same Colors and same sets (except C5 (kings)
    */

    class FourColorAlgo
    {
        public List<string> deck = new List<string>();
        public Dictionary<int, List<string>> playersHands = new Dictionary<int, List<string>>();
        public int sets = 5;

        static void Main(string[] args)
        {
            FourColorAlgo game = new FourColorAlgo();  // Create an instance of the class

            //Console.Write("Sets = ");
            //game.sets = int.Parse(Console.ReadLine());  
            Console.WriteLine("Number of sets = " + game.sets);

            for (int i = 1; i <= game.sets; i++)
            {
                game.playersHands[i] = new List<string>();  
            }

            //game.playersHands[1].Add("C1");
            //game.playersHands[1].Add("C3");
            //game.playersHands[1].Add("C4");

     
            //game.playersHands[1].Add("C3");
            game.playersHands[1].Add("C1");
            game.playersHands[1].Add("C1_Red");
            game.playersHands[1].Add("C1_Yellow");
            //game.playersHands[1].Add("C1");

            game.playersHands[1].Add("C2");
            game.playersHands[1].Add("C2_Red");
            game.playersHands[1].Add("C2_Yellow");


            game.playersHands[1].Add("C4");
            game.playersHands[1].Add("C4");
            game.playersHands[1].Add("C4_Green");
            game.playersHands[1].Add("C3_Red");
            game.playersHands[1].Add("C3_Yellow");


            //game.playersHands[1].Add("C5");
            //game.playersHands[1].Add("C5_Green");
            //game.playersHands[1].Add("C5_Green");
            game.playersHands[1].Add("C5_Red");
            //game.playersHands[1].Add("C5_Yellow");
            //game.playersHands[1].Add("C5_Red");

            game.playersHands[1].Add("C6");
            game.playersHands[1].Add("C6_Red");
            game.playersHands[1].Add("C6_Yellow");
            //game.playersHands[1].Add("C4_Red");

            game.playersHands[1].Add("C7_Red");
            game.playersHands[1].Add("C7_Yellow");
            game.playersHands[1].Add("C7_Green");
            game.playersHands[1].Add("C7");
            //game.playersHands[1].Add("C6_Red");



            game.playersHands[2] = new List<string> { "C1_Yellow", "C3_Yellow", "C4_Yellow", "C2_Green", "C5_Green", "C6_Green", "C2", "C5", "C6","C1_Red" };
            game.playersHands[3] = new List<string> { "C6", "C6_Green", "C6_Yellow","C5","C4_Green","C4","C4_Red"};
            game.playersHands[4] = new List<string> { "C1", "C3", "C4", "C1_Red", "C3_Red", "C4_Red", "C1_Green", "C3_Green", "C4_Green", "C1_Yellow", "C3_Yellow", "C4_Green", "C1_Green", "C3", "C4" };
            game.playersHands[5] = new List<string> { "C1", "C3", "C4", "C1_Red", "C3_Red", "C4_Red", "C1_Green", "C3_Green", "C4_Green", "C1_Yellow", "C3_Yellow", "C4_Green", "C1_Green", "C3", "C4_Red" };

            List<string> focustile = game.playersHands[1];

            //List<List<string>> H = game.GetHonourSets(focustile);
            //List<List<string>> CH = GetCombinations(H);


            //Console.WriteLine("hand contains : " + String.Join(", ", focustile));
            //Console.WriteLine($"The hand 1 is {game.CheckColorSets(focustile)} ");

            Console.WriteLine($"AFTER LOGIC : { game.WinCondition(1) }" );
        }

        public string WinCondition(int x)
        {
            List<string> focustile = new List<string>(playersHands[x]);
            List<List<string>> H = GetHonourSets(focustile);
            List<List<string>> CH = GetCombinations(H);

            if(CH.Count == 0)
            {
                return "No Honour";
            }

            foreach (var h in CH)
            {
                focustile = new List<string>(playersHands[x]);
                Console.WriteLine("Combi : " + String.Join(",", h));
                foreach (var item in h)
                {
                    focustile.Remove(item);
                }
                
                
                if (CheckColorSets(focustile)) { return "WIN"; }
            }

            return "x";
        }

        public bool CheckColorSets(List<string> remainer)
        {
            if (remainer.Count == 0)
            {
                return true;
            }

            //Remove Kings
            remainer.RemoveAll(item => item.Contains("C5"));


            Console.WriteLine("REMAINER : " + String.Join(",", remainer));
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
            if (RemovesetsItems(ref remainer, C4List)) { return CheckColorSets(remainer);}
            //Check C6
            List<string> C6List = new List<string> { "C6", "C6_Green", "C6_Red", "C6_Yellow" };
            if (RemovesetsItems(ref remainer, C6List)) { return CheckColorSets(remainer); }
            //Check C7
            List<string> C7List = new List<string> { "C7", "C7_Green", "C7_Red", "C7_Yellow" };
            if (RemovesetsItems(ref remainer, C7List)) { return CheckColorSets(remainer); }

            
            return false;
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
            } else {  
                return false; 
            }

        }


        // Honor sets
        private List<List<string>> GetHonourSets(List<string> tile)
        {
            List<List<string>> honourSets = new List<List<string>>();
            List<List<string>> possibleHonours = new List<List<string>>()
            {
                new List<string> { "C2", "C5", "C7" },  // KQB
                new List<string> { "C2_Green", "C5_Green", "C7_Green" },
                new List<string> { "C2_Yellow", "C5_Yellow", "C7_Yellow" },
                new List<string> { "C2_Red", "C5_Red", "C7_Red" },

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

        public static List<List<string>> GetCombinations(List<List<string>> lists)
        {
            List<List<string>> result = new List<List<string>>();

            for (int i = 1; i <= lists.Count; i++)
            {
                var subsets = GetSubsets(lists, i);
                result.AddRange(subsets);
            }

            return result;
        }

        public static List<List<string>> GetSubsets(List<List<string>> lists, int subsetSize)
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


    }
}
