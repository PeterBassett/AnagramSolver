using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Combinatorics.Collections;

namespace AnagramSolverLib
{
    public class CrosswordAnagramService
    {
        public event Action<string, long> OnPermutationCount;
        public event Action<string, double> OnPercentageComplete;
        public event Action<string, string[]> OnSearchComplete;
        public event Action<string, string[]> OnBestGuesses;

        public void CrosswordAnagramSolver(string tag, string clue, string availableLetters, Dictionary<string, int> wordList)
        {
            if (clue.Length == 0 || availableLetters.Length == 0)
                return;

            var words = clue.Split(new[] { ' ' });

            var permutations = new Permutations<char>(availableLetters.ToLower().ToCharArray(), GenerateOption.WithoutRepetition);

            OnPermutationCount(tag, permutations.Count);

            int permutationCount = 0;
            var answerSet = new Dictionary<string, int>();

            foreach (IList<char> p in permutations)
            {
                permutationCount++;
                var permutation = new Queue<char>(p);

                var foundWords = new List<Tuple<string, int>>();
                bool foundAll = true;
                foreach (var word in words)
                {
                    var arr = word.ToCharArray();

                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (arr[i] == '?')
                            arr[i] = permutation.Dequeue();
                    }

                    var testWord = new string(arr);
                    if (wordList.ContainsKey(testWord))
                    {
                        foundWords.Add(new Tuple<string, int>(testWord, wordList[testWord]));
                    }
                    else
                    {
                        foundAll = false;
                        break;
                    }
                }

                if (permutationCount % 500000 == 0)
                {
                    OnPercentageComplete(tag, permutationCount / (double)permutations.Count * 100);
                }

                if (foundAll)
                {
                    var answer = string.Join(" ", foundWords.Select(w => w.Item1).ToArray());
                    var frequency = foundWords.Sum(w => w.Item2);

                    if (answerSet.ContainsKey(answer))
                        continue;

                    answerSet.Add(answer, frequency);

                    OnBestGuesses(tag, answerSet.OrderByDescending(a => a.Value).Take(15).Select(a => a.Key).ToArray());
                }
            }

            OnPercentageComplete(tag, 100);

            var orderedAnswers = answerSet.OrderByDescending(a => a.Value);
            OnSearchComplete(tag, orderedAnswers.OrderByDescending(a => a.Value).Take(15).Select(a => a.Key).ToArray());
        }
    }
}
