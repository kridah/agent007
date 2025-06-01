using OllamaSharp;

namespace Agent007.Tools
{
    /// <summary>
    /// Random number generation tools for LLM agents
    /// </summary>
    public class RandomTools
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Simulate a dice roll with the specified number of sides
        /// </summary>
        /// <param name="sides">Number of sides on the dice (default: 6)</param>
        /// <returns>The result of the dice roll</returns>
        [OllamaTool]
        public static int RollDice(int sides = 6)
        {
            return _random.Next(1, sides + 1);
        }
    }
}