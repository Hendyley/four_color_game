using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace Program
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

    class FourColorAlgo
    {
        public List<string> Deck = new List<string>();
        public Dictionary<int, List<string>> playersHands = new Dictionary<int, List<string>>();
        public int sets = 5;
        public string AItile;
        public int combipairs = 0;
        public List<List<string>> Hands; // index 0, 1, ..., N-1
        public int AITurn;               // index of AI in Hands
        public int CurrentTurn;

        static void Main(string[] args)
        {
            FourColorAlgo game = new FourColorAlgo();  // Create an instance of the class

            List<string> Deck = game.CreateDeck();
            game.ShuffleDeck();
            List<string> hand1 = new List<string>();
            List<string> hand2 = new List<string>();
            List<string> table = new List<string>();
            List<string> Ifhand = new List<string>();
            bool over = false;
            string tile,des;


            //for (int i = 1; i < 6; i++)
            //{
            //    hand1.Add(game.DrawTile(Deck, ""));
            //    hand2.Add(game.DrawTile(Deck, ""));
            //}

            //hand1.Add(game.DrawTile(Deck, ""));

            //hand1.Sort();
            //hand2.Sort();
            //Console.WriteLine($"hand1 {hand1.Count} = {String.Join(",", hand1)}");
            //Console.WriteLine($"hand2 {hand2.Count} = {String.Join(",", hand2)}");
            //Console.WriteLine($"hand1 win? {game.WinCondition(hand1)}");
            //Console.WriteLine($"hand2 win? {game.WinCondition(hand2)}");

            //while (!over)
            //{
            //    GameState state1 = new GameState()
            //    {
            //        Hand = hand1.ToList(),
            //        Deck = Deck.ToList(),
            //        Table = table.ToList(),
            //    };
            //    string aiSuggestion = game.ExpectimaxDecision(state1, 2);
            //    Console.WriteLine("hand1 throw Tile " + $"AI suggests for hand1: {aiSuggestion}");
            //    tile = Console.ReadLine();

            //    hand1.Remove(tile);
            //    table.Add(tile);


            //    Ifhand = hand2.ToList();
            //    Ifhand.Add(tile);
            //    var AIdes = "";
            //    if (game.EvaluateState((new GameState() { Hand = Ifhand })) > game.EvaluateState((new GameState() { Hand = hand2 })))
            //        AIdes = "YES";
            //    else
            //        AIdes = "NO";

            //    Console.WriteLine($"hand2 pick Tile?   {game.WinCondition(Ifhand)} AI Suggested : {AIdes}");
            //    des = Console.ReadLine();
            //    if (des.StartsWith("n"))
            //    {
            //        hand2.Add(game.DrawTile(Deck, ""));
            //        //hand2.Add(game.DrawTile(Deck, ""));
            //    }
            //    else
            //    {
            //        table.Remove(tile);
            //        hand2.Add(tile);
            //    }

            //    hand1.Sort();
            //    hand2.Sort();
            //    Console.WriteLine($"hand1 {hand1.Count} = {String.Join(",", hand1)}");
            //    Console.WriteLine($"hand2 {hand2.Count} = {String.Join(",", hand2)}");
            //    Console.WriteLine($"hand1 win? {game.WinCondition(hand1)}");
            //    Console.WriteLine($"hand2 win? {game.WinCondition(hand2)}");


            //    GameState state2 = new GameState()
            //    {
            //        Hand = hand2.ToList(),
            //        Deck = Deck.ToList(),
            //        Table = table.ToList(),
            //    };
            //    string aiSuggestion2 = game.ExpectimaxDecision(state2, 2);
            //    Console.WriteLine("hand2 throw Tile " + $"AI suggests for hand2: {aiSuggestion2}");
            //    tile = Console.ReadLine();
            //    hand2.Remove(tile);
            //    table.Add(tile);


            //    Ifhand = hand1.ToList();
            //    Ifhand.Add(tile);
            //    AIdes = "";
            //    if (game.EvaluateState((new GameState() { Hand = Ifhand })) > game.EvaluateState((new GameState() { Hand = hand1 })))
            //        AIdes = "YES";
            //    else
            //        AIdes = "NO";

            //    Console.WriteLine($"hand1 pick Tile?   {game.WinCondition(Ifhand)} AI Suggested : {AIdes}");
            //    des = Console.ReadLine();
            //    if (des.StartsWith("n"))
            //    {
            //        hand1.Add(game.DrawTile(Deck, ""));
            //        //hand1.Add(game.DrawTile(Deck, ""));
            //    }
            //    else
            //    {
            //        table.Remove(tile);
            //        hand1.Add(tile);
            //    }

            //    hand1.Sort();
            //    hand2.Sort();
            //    Console.WriteLine($"hand1 {hand1.Count} = {String.Join(",", hand1)}");
            //    Console.WriteLine($"hand2 {hand2.Count} = {String.Join(",", hand2)}");
            //    Console.WriteLine($"hand1 win? {game.WinCondition(hand1)}");
            //    Console.WriteLine($"hand2 win? {game.WinCondition(hand2)}");

            //    //over = true;
            //}

            GameState GS1 = new GameState()
            {
                Hand = new List<string> {
                    "C1", "C3", "C4",
                    "C1_Green", "C3_Green", "C4_Green",
                    "C1_Red", "C3_Red", "C4_Red", "C4"
                },
                Deck = new List<string> {
                    "C2", "C2_Red", "C2_Green", "C2_Yellow", "C7", "C5"
                },
                Table = new List<string>(),
            };

            string bestMove = game.MAX_AI_DISCARD(GS1); // Adjust depth if needed
            Console.WriteLine($"Best move decided by AI: {bestMove}");



        }

        private List<string> CreateDeck()
        {
            List<string> newDeck = new List<string>();
            String x;
            for (int s = 0; s < 4; s++)
            {
                for (int i = 1; i <= 7; i++) // 7 Pieces
                {
                    x = $"C{i}";
                    newDeck.Add(x);
                    newDeck.Add(x + "_Green");
                    newDeck.Add(x + "_Red");
                    newDeck.Add(x + "_Yellow");
                }
            }
            Console.WriteLine("Amount in the Deck: " + newDeck.Count);

            return newDeck;
        }

        private void ShuffleDeck()
        {
            Random rng = new Random();
            int n = Deck.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (Deck[k], Deck[n]) = (Deck[n], Deck[k]);
            }
        }

        private string DrawTile(List<string> Deck,string x)
        {

            if (Deck.Count > 0)
            {
                if(x=="")
                {
                    string tilevalue = Deck[0];
                    Deck.RemoveAt(0);
                    return tilevalue;
                } 
                else
                {
                    string tilevalue = x;
                    Deck.Remove(x);
                    return tilevalue;
                }

            }
            else
            {
                Console.WriteLine("Deck Empty");
                return "";
            }
        }

        /// <summary> AI
        /// 
        public class GameState
        {
            public List<string> Hand;
            public List<string> Deck;
            public List<string> Table;
            public int Score;
        }

        public double EvaluateState(GameState state)
        {
            if (WinCondition(state.Hand) == "WIN")
                return 10000;

            double bestscore = 0;
            double score = 0;
            double remainers = 0;

            var focustile = state.Hand.ToList();
            List<List<string>> H = GetHonourSets(focustile);
            List<List<string>> CH = GetCombinations(H);

            foreach (var h in CH)
            {
                focustile = state.Hand.ToList();
                foreach (var item in h)
                {
                    focustile.Remove(item);
                }

                remainers = focustile.Count();
                double colorPenalty = CheckColorSets(focustile);
                score = h.Count * 10 - remainers - colorPenalty;  // Adjust weights to emphasize better set formations
                bestscore = Math.Max(bestscore, score);

                //remainers = focustile.Count();
                //score = h.Count + remainers - CheckColorSets(focustile);
                //bestscore = Math.Max(bestscore, score);
                //Console.WriteLine($"Combi of {h.Count} + {remainers - CheckColorSets(focustile)} = {score} : {String.Join(",", h)}  Best Score = {bestscore}");
            }

            return bestscore;
        }

        public string MAX_AI_DISCARD (GameState state, int depth = 2)
        {
            if (WinCondition(state.Hand) == "WIN")
                return "";

            double handscore = double.NegativeInfinity;
            string tiletodiscard = "";
            foreach(var tile in state.Hand)
            {
                var focustile = state.Hand.ToList();
                focustile.Remove(tile);
                double score = EvaluateState( new GameState() { Hand= focustile } );
                if( score > handscore)
                {
                    tiletodiscard = tile;
                    handscore = score;
                }
            }
            return tiletodiscard;
        }

        //public string ExpectimaxDecision(GameState state, int depth)
        //{
        //    string bestMove = null;
        //    double bestValue = double.NegativeInfinity;

        //    foreach (var move in GetLegalMoves(state))
        //    {
        //        GameState newState = SimulateMove(state, move);
        //        double value = Expectimax(newState, depth - 1, false);
        //        if (value > bestValue)
        //        {
        //            bestValue = value;
        //            bestMove = move;
        //        }
        //    }

        //    return bestMove;
        //}

        //public double Expectimax(GameState state, int depth, bool isAITurn)
        //{
        //    if (depth == 0 || WinCondition(state.AIHand) == "WIN")
        //        return EvaluateState(state);

        //    if (isAITurn)
        //    {
        //        double maxEval = double.NegativeInfinity;
        //        foreach (var move in GetLegalMoves(state))
        //        {
        //            GameState newState = SimulateMove(state, move);
        //            double eval = Expectimax(newState, depth - 1, false);
        //            maxEval = Math.Max(maxEval, eval);
        //        }
        //        return maxEval;
        //    }
        //    else
        //    {
        //        double expectedValue = 0;
        //        var draws = GetPossibleOpponentDraws(state.Deck);
        //        foreach (var draw in draws)
        //        {
        //            double probability = 1.0 / draws.Count;
        //            GameState newState = SimulateOpponentTurn(state, draw);
        //            expectedValue += probability * Expectimax(newState, depth - 1, true);
        //        }
        //        return expectedValue;
        //    }
        //}
        //public List<string> GetLegalMoves(GameState state)
        //{
        //    // Simply return all tiles in AIHand as legal moves
        //    return state.AIHand;
        //}

        //public GameState SimulateMove(GameState state, string move)
        //{
        //    var newState = new GameState
        //    {
        //        AIHand = state.AIHand.ToList(),
        //        Deck = state.Deck.ToList(),
        //        Table = state.Table.ToList(),
        //    };

        //    newState.AIHand.Remove(move);
        //    newState.Table.Add(move);
        //    return newState;
        //}

        //public List<string> GetPossibleOpponentDraws(List<string> deck)
        //{
        //    return deck.Distinct().ToList();
        //}

        //public GameState SimulateOpponentTurn(GameState state, string draw)
        //{
        //    var newState = new GameState
        //    {
        //        AIHand = state.AIHand.ToList(),
        //        Deck = state.Deck.ToList(),
        //        Table = state.Table.ToList(),
        //    };

        //    newState.Deck.Remove(draw);
        //    // Simulate opponent keeping the card, we don't model opponent hand so it's just state cost increase
        //    return newState;
        //}

        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>

        public string WinCondition(List<string> v)
        {
            var focustile = v.ToList();
            List<List<string>> H = GetHonourSets(focustile); // Default Honours
            List<List<string>> CH = GetCombinations(H); // All Possible Honours Sets

            if(CH.Count == 0)
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

                if (CheckColorSets(focustile)==0) 
                { 
                    return "WIN"; 
                }
            }

            return "X";
        }

        public int CheckColorSets(List<string> remainer)
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
            if (RemovesetsItems(ref remainer, C4List)) { return CheckColorSets(remainer);}
            //Check C6
            List<string> C6List = new List<string> { "C6", "C6_Green", "C6_Red", "C6_Yellow" };
            if (RemovesetsItems(ref remainer, C6List)) { return CheckColorSets(remainer); }
            //Check C5
            List<string> C5List = new List<string> { "C5", "C5_Green", "C5_Red", "C5_Yellow" };
            if (RemovesetsItems(ref remainer, C5List)) { return CheckColorSets(remainer); }


            return remainer.Count;
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
