using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using Combinatorics;
using Combinatorics.Collections;

namespace Anagram
{
    class Program
    {
        public enum Mode
        {
            None,
            AnagramSolver,
            SimilarWord,
            CrosswordAnagram // to solve clues given in an android game my wife has
        }

        static void Main(string[] args)
        {
            Mode mode;
            do
            {
                mode = GetExecutionMode();

                RunMode(mode);
            } while (mode != Mode.None);
        }

        private static void RunMode(Mode mode)
        {
            if (mode == Mode.None)
                return;

            var wordslist = LoadWordFrequencies();

            switch (mode)
            {
                case Mode.AnagramSolver:
                    {
                        var tree = BuildTrie(wordslist.Keys.Cast<string>());
                        AnagramSolver(tree);
                        break;
                    }
                case Mode.SimilarWord:
                    {
                        SimilarWords(new HashSet<string>(wordslist.Keys.Cast<string>()));
                        break;
                    }
                case Mode.CrosswordAnagram:
                    {

                        CrosswordAnagramSolver(wordslist);
                        break;
                    }
                default:
                    break;
            }
        }

        private static Mode GetExecutionMode()
        {
            Console.WriteLine("1 = Anagram Solver");
            Console.WriteLine("2 = Crossword Anagram Solver");
            Console.WriteLine("3 = Similar Words");
            Console.WriteLine("Press any other key to exit");

            var answer = Console.ReadKey();

            Console.WriteLine("");

            if (answer.KeyChar == '1')
                return Mode.AnagramSolver;
            else if (answer.KeyChar == '2')
                return Mode.CrosswordAnagram;
            else if (answer.KeyChar == '3')
                return Mode.SimilarWord;
            else
                return Mode.None;
        }

        private static LetterTree BuildTrie(IEnumerable<string> words)
        {
            Console.WriteLine("Populating Letter Tree");

            LetterTree root = new LetterTree();

            foreach (string word in words)
                root.AddWord(word);

            return root;
        }

        private static void AnagramSolver(LetterTree root)
        {
            do
            {
                Console.WriteLine("----------------------------------------------------------------------------");
                Console.WriteLine("Enter anagram to solve. Type EXIT to exit back to the main menu.");
                string data = Console.ReadLine();

                if (data.Trim() == "EXIT")
                    break;

                if (data.Trim().Length == 0)
                    continue;

                int iX = Console.CursorLeft;
                int iY = Console.CursorTop;
                var iCount = 0;
                var lockable = new object();
                Action<string> SearchProgress = (word) =>
                {
                    iCount++;

                    if (iCount % 5000 == 0)
                    {
                        lock (lockable)
                        {
                            Console.CursorLeft = iX;
                            Console.CursorTop = iY;

                            Console.WriteLine(word.PadRight(40));
                        }
                    }
                };

                var oOutput = from s in root.Search(data, SearchProgress)
                              let score = ScrabbleWordScore(s)
                              //where score > 10 || s.Length > 6
                              orderby s.Length descending
                              select s;

                oOutput = oOutput.ToArray();

                Console.WriteLine("ANSWERS!");
                Console.WriteLine("-------------------------------------------");
                foreach (var strWord in oOutput)
                {
                    Console.WriteLine("{0}", strWord);
                }

            } while (true);
        }

        private static void CrosswordAnagramSolver(Dictionary<string, int> wordList)
        {
            do
            {
                Console.WriteLine("----------------------------------------------------------------------------");
                Console.WriteLine("Enter clue. '?' for missing letters and space to separate words. Type EXIT to exit back to the main menu.");
                string clue = Console.ReadLine();
                clue = clue.Trim();
                if (clue == "EXIT")
                    break;

                Console.WriteLine("Enter the set of available letters. Type EXIT to exit back to the main menu");
                string availableLetters = Console.ReadLine();
                availableLetters = availableLetters.Trim();
                if (availableLetters == "EXIT")
                    break;

                if (clue.Length == 0 || availableLetters.Length == 0)
                    continue;
                               
                var words = clue.Split(new[] { ' ' });

                var permutations = new Permutations<char>(availableLetters.ToLower().ToCharArray(), GenerateOption.WithoutRepetition);

                Console.WriteLine("Searching {0:n0} permutations", permutations.Count);

                int currentLine = Console.CursorTop;
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
                        Console.CursorTop = currentLine + 1;
                        Console.CursorLeft = 0;
                        Console.Write("{0:n0} permutations searched. {1:n0}% complete", permutationCount, permutationCount / (double)permutations.Count * 100);
                    }

                    if (foundAll)
                    {
                        var answer = string.Join(" ", foundWords.Select(w => w.Item1).ToArray());
                        var frequency = foundWords.Sum(w => w.Item2);

                        if (answerSet.ContainsKey(answer))
                            continue;

                        answerSet.Add(answer, frequency);

                        Console.CursorTop = currentLine + 1;
                        Console.CursorLeft = 0;

                        foreach (var item in answerSet.OrderByDescending(a => a.Value).Take(15))
                        {
                            Console.WriteLine(item);
                        }
                    }
                }

                var orderedAnswers = answerSet.OrderByDescending(a => a.Value);
                Console.CursorTop = currentLine;
                Console.CursorLeft = 0;
                Console.WriteLine("Search Complete");
                foreach (var answer in orderedAnswers)
                {
                    Console.WriteLine("FOUND - " + answer.Key + "\t\t\t" + answer.Value);
                }

            } while (true);
        }

        private static Dictionary<string, int> LoadWordFrequencies()
        {
            //if (!File.Exists(@"AllWords.zip"))
            // the source file for this was deleted as it took up too much space.
            //    BuildWordFrequencyFile();

            var archive = System.IO.Compression.ZipFile.OpenRead(@"AllWords.zip");

            var entry = archive.GetEntry("AllWords.txt");

            var linesFromFile = new List<string>();
            using (var reader = new StreamReader(entry.Open()))
            {
                while (!reader.EndOfStream)
                    linesFromFile.Add(reader.ReadLine());
            }

            return (from line in linesFromFile
                    let words = line.Split('\t')
                    select new Tuple<string, int>(words[0], int.Parse(words[1]))).ToDictionary(t => t.Item1, t => t.Item2);
        }
       
        private static void SimilarWords(HashSet<string> wordslist)
        {
            var candidateStrings = new List<string>();
            string entry;

            while (true)
            {
                Console.WriteLine("Enter a word to examine. Blank to skip");
                entry = Console.ReadLine();

                if (entry.Length == 0)
                    break;

                candidateStrings.Add(entry);
            }

            var candidates = candidateStrings.ToDictionary(c => c, c => new SortedList<int, List<string>>());

            foreach (var candidate in wordslist)
            {
                foreach (var target in candidates.Keys)
                {
                    var distance = Distance(target, candidate);

                    if (distance > 3)
                        continue;

                    var list = candidates[target];

                    if (!list.ContainsKey(distance))
                        list.Add(distance, new List<string>());

                    list[distance].Add(candidate);

                    if (list.Count > 10)
                        list.RemoveAt(list.Count - 1);
                }
            }

            foreach (var target in candidates.Keys)
            {
                Console.WriteLine(target);
                var words = candidates[target];
                foreach (var score in words.Keys)
                {
                    Console.WriteLine("\tScore {0}", score);
                    foreach (var word in words[score])
                    {
                        Console.WriteLine("\t\t{0}", word);
                    }
                }
            }
        }

        private static int Distance(string a, string b)
        {
            int aLength = a.Length;
            int bLength = b.Length;

            int[,] distances = new int[aLength + 1, bLength + 1];

            if (aLength == 0)
                return bLength;

            if (bLength == 0)
                return aLength;

            for (int i = 0; i <= aLength; i++)
                distances[i, 0] = i;

            for (int j = 0; j <= bLength; j++)
                distances[0, j] = j;

            for (int i = 1; i <= aLength; i++)
            {
                for (int j = 1; j <= bLength; j++)
                {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;

                    distances[i, j] = Math.Min(
                                        Math.Min(
                                            distances[i - 1, j] + 1,
                                            distances[i, j - 1] + 1
                                        ),
                                        distances[i - 1, j - 1] + cost
                                     );
                }
            }

            return distances[aLength, bLength];
        }

        private static int ScrabbleWordScore(string strWord)
        {
            return (from l in strWord.ToUpper().ToCharArray()
                    select ScrabbleLetterScore(l)).Sum();
        }

        private static int ScrabbleLetterScore(char letter)
        {
            /*
               1 point: E ×12, A ×9, I ×9, O ×8, N ×6, R ×6, T ×6, L ×4, S ×4, U ×4
               2 points: D ×4, G ×3
               3 points: B ×2, C ×2, M ×2, P ×2
               4 points: F ×2, H ×2, V ×2, W ×2, Y ×2
               5 points: K ×1
               8 points: J ×1, X ×1
               10 points: Q ×1, Z ×1
            */

            switch (letter)
            {
                case 'E':
                case 'A':
                case 'I':
                case 'O':
                case 'N':
                case 'R':
                case 'T':
                case 'L':
                case 'S':
                case 'U':
                    return 1;
                case 'D':
                case 'G':
                    return 2;
                case 'B':
                case 'C':
                case 'M':
                case 'P':
                    return 3;
                case 'F':
                case 'H':
                case 'V':
                case 'W':
                case 'Y':
                    return 4;
                case 'K':
                    return 5;
                case 'J':
                case 'X':
                    return 8;
                case 'Q':
                case 'Z':
                    return 10;
                default:
                    throw new ApplicationException("Unaccounted letter " + letter.ToString()); ;
            }
        }

        /*
        #region unused functionality
        // These functions were used in the building of the AllWords.zip file.

        private static void BuildWordFrequencyFile()
        {
            var wordList = LoadWordList();
            var frequencies = ProcessFrequencies();

            var wordFrequencies = from word in wordList
                                  select new
                                  {
                                      Word = word,
                                      Frequency = frequencies.ContainsKey(word) ? frequencies[word] : -1
                                  };

            var lines = from word in wordFrequencies
                        select string.Format("{0}\t{1}", word.Word, word.Frequency);

            File.WriteAllLines("AllWords.txt", lines.ToArray());

            using (var fs = new FileStream("AllWords.zip", FileMode.Create))
            using (var archive = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("AllWords.txt");
                using (var output = entry.Open())
                using (var input = File.Open("AllWords.txt", FileMode.Open, FileAccess.Read))
                    input.CopyTo(output);
            }

            Console.WriteLine("{0} words loaded", lines.Count());
        }

        static HashSet<string> LoadWordsFromArchive(Action<bool, string> Progress)
        {
            string cleanse(string data)
            {
                return data.ToLower().Trim();
            }

            var words = new HashSet<string>();

            using (var archive = System.IO.Compression.ZipFile.OpenRead("RawWords.zip"))
            {
                foreach (var entry in archive.Entries)
                {
                    using (var stream = new StreamReader(entry.Open()))
                    {
                        while (!stream.EndOfStream)
                        {
                            var word = stream.ReadLine();

                            var cleansedWord = cleanse(word);

                            if (string.IsNullOrEmpty(cleansedWord))
                                continue;

                            if (!words.Contains(cleansedWord))
                            {
                                Progress?.Invoke(true, cleansedWord);

                                words.Add(cleansedWord);
                            }
                        }
                    }
                }
            }
            return words;
        }


        private static HashSet<string> LoadWordList()
        {
            Console.Write("Loading words... ");

            int iX = Console.CursorLeft;
            int iY = Console.CursorTop;
            int iCount = 0;
            Action<bool, string> LoadProgress = (include, word) =>
            {
                iCount++;

                if (iCount % 1000 == 0)
                {
                    Console.CursorLeft = iX;
                    Console.CursorTop = iY;

                    if (include)
                        Console.WriteLine(word.PadRight(40));
                }
            };

            HashSet<string> words = LoadWordsFromArchive(LoadProgress);

            iCount = -1;
            LoadProgress(true, "");

            Console.WriteLine("{0} words loaded", words.Count);

            return words;
        }

        private static Dictionary<string, int> ProcessFrequencies()
        {
            var lines = File.ReadAllLines(@"..\..\1_1_all_fullalpha.txt");

            bool shouldIgnore(string word)
            {
                if (word.Contains("-"))
                    return true;

                if (word.Contains("/"))
                    return true;

                if (word.Contains("*"))
                    return true;

                if (word.Contains("+"))
                    return true;

                if (word.Contains("."))
                    return true;

                if (word.Contains("&"))
                    return true;

                return false;
            }

            var words = new Dictionary<string, int>();

            foreach (var line in lines)
            {
                var parts = line.Split('\t');

                var frequency = int.Parse(parts[4]);

                if (frequency == 0)
                    continue;

                var word = parts[1];

                if (shouldIgnore(parts[1]))
                    continue;

                if (parts[1] == "@")
                {
                    if (shouldIgnore(parts[3]))
                        continue;

                    word = parts[3];
                }

                if (word.StartsWith("'") || word.EndsWith("'"))
                    continue;

                if (word.Any((c) => !char.IsLetter(c)))
                    continue;

                word = word.ToLower();

                if (!words.ContainsKey(word))
                    words.Add(word, frequency);
                else
                {
                    var currentFrequency = words[word];

                    if (currentFrequency < frequency)
                        words[word] = frequency;
                }
            }

            return words;
        }
        #endregion*/
    }
}
