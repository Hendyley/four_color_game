using FourColors;
using System;
using System.Collections.Generic;


// Honors
/*
C6, C6.Green, C6.Yellow, C6_Red  (Any 3)     P
C1, C3, C4 (Same Color of this sets)         HRC
C2, C5, C7 (Same Color of this sets)         KQB
*/
// Sets
/*
Same Colors and same sets (except C7 (kings)
*/
namespace Program
{
    class FourColorAlgo
    {
        static void Main(string[] args)
        {
            GameLogic.Deck = new List<string>();
            GameLogic.Table = new List<string>();
            Dictionary <int, List<string>> playersHands = new Dictionary<int, List<string>>();
            playersHands[1] = new List<string>{};
            playersHands[2] = new List<string>{};

            GameLogic.Deck.AddRange("C2,C3".Split(',').Select(s => s.Trim()));
            GameLogic.Table.AddRange("C1,C1,C1,C1,C1_Yellow,C1_Yellow,C1_Yellow,C1_Yellow".Split(',').Select(s => s.Trim()));

            playersHands[2].Add("C1");
            playersHands[2].Add("C2_Red");
            playersHands[2].Add("C3_Yellow");
            playersHands[2].Add("C4_Green");
            playersHands[2].Add("C1");
            playersHands[2].Add("C2_Red");
            playersHands[2].Add("C3_Yellow");
            playersHands[2].Add("C4_Green");
            playersHands[2].Add("C1");
            playersHands[2].Add("C2_Red");
            playersHands[2].Add("C3_Yellow");
            playersHands[2].Add("C4_Green");
            playersHands[2].Add("C5_Yellow");
            playersHands[2].Add("C5_Yellow");
            playersHands[2].Add("C5_Yellow");


            playersHands[1].Add("C1_Green");
            playersHands[1].Add("C1_Red");
            playersHands[1].Add("C1_Yellow");

            playersHands[1].Add("C2_Green");
            playersHands[1].Add("C2_Red");
            playersHands[1].Add("C2_Yellow");
            playersHands[1].Add("C2");

            playersHands[1].Add("C1_Green");
            playersHands[1].Add("C3_Green");
            playersHands[1].Add("C4_Green");

            playersHands[1].Add("C6_Green");
            playersHands[1].Add("C6_Red");
            playersHands[1].Add("C6_Yellow");
            playersHands[1].Add("C6");

            playersHands[1].Add("C7");

            GameLogic.GameState x = new GameLogic.GameState();
            x.Hand = playersHands[2].ToList();

            GameLogic.GameState x2 = new GameLogic.GameState();
            x2.Hand = playersHands[2].ToList();
            x2.Hand.Add("C4");


            string discard = GameLogic.MAX_AI_DISCARD(new GameLogic.GameState { Hand = new List<string>(x2.Hand.ToList()) });
            Console.WriteLine($"AI suggests discarding: {discard}");

            Console.WriteLine($"Number of hands is {playersHands[2].Count}");
            Console.WriteLine($"Score {GameLogic.EvaluateState(x)} vs {GameLogic.EvaluateState(x2)}");

            string result = GameLogic.WinCondition(playersHands[2]);
            Console.WriteLine($"AFTER LOGIC: {result}");

            bool castle = GameLogic.CheckCastle(playersHands[2]);
            Console.WriteLine($"CASTLE: {castle}");


        }
    }
}
