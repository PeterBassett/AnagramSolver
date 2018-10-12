using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnagramSolverLib
{
    public class WordListLoader
    {
        public Dictionary<string, int> LoadWordFrequencies(string wordArchivePath)
        {
            var archive = System.IO.Compression.ZipFile.OpenRead(wordArchivePath);

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
    }
}
