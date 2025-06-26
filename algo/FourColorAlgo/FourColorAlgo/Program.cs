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
Same Colors and same sets (except C5 (kings)
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

            GameLogic.Deck.AddRange("C2,C3".Split(',').Select(s => s.Trim()));
            GameLogic.Table.AddRange("C1,C1,C1,C1,C1_Yellow,C1_Yellow,C1_Yellow,C1_Yellow".Split(',').Select(s => s.Trim()));


            playersHands[1].Add("C1");
            playersHands[1].Add("C1_Red");
            playersHands[1].Add("C1_Green");
            playersHands[1].Add("C1_Yellow");

            playersHands[1].Add("C6");
            playersHands[1].Add("C6_Yellow");
            playersHands[1].Add("C6_Green");

            playersHands[1].Add("C7");

            playersHands[1].Add("C5_Green");
            playersHands[1].Add("C7_Green");
            playersHands[1].Add("C2_Red");
            playersHands[1].Add("C7_Red");

            playersHands[1].Add("C7_Red");
            playersHands[1].Add("C5_Red");
            playersHands[1].Add("C2_Red");


            string discard = GameLogic.MAX_AI_DISCARD(new GameLogic.GameState { Hand = new List<string>(playersHands[1].ToList()) });
            Console.WriteLine($"AI suggests discarding: {discard}");


            GameLogic.GameState x = new GameLogic.GameState();
            x.Hand = playersHands[1].ToList();

            GameLogic.GameState x2 = new GameLogic.GameState();
            x2.Hand = playersHands[1].ToList();


            string result = GameLogic.WinCondition(playersHands[1]);
            Console.WriteLine($"AFTER LOGIC: {result}");

            bool castle = GameLogic.CheckCastle(playersHands[1]);
            Console.WriteLine($"CASTLE: {castle}");


        }
    }
}
