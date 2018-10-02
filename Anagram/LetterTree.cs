using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Combinatorics;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Combinatorics.Collections;

namespace Anagram
{
    public class LetterNode
    {
        public static int MaxDepthSearched = 0;

        public char Letter;
        public Dictionary<char, LetterNode> Children;
        public bool IsCompleteWord = false;
        public bool WordEnumerated = false;

        public LetterNode()
        {
            Letter = char.MaxValue;
        }

        public LetterNode(char cLetter)
        {
            Letter = cLetter;
        }

        public void StoreMaxDepth(int iDepth)
        {
            if (MaxDepthSearched < iDepth)
                MaxDepthSearched = iDepth;
        }

        public bool IsTerminator
        {
            get
            {
                if (Children == null)
                    return true;

                if (Children.Count == 0)
                    return true;

                return false;
            }
        }

        public void AddWord(string strWord, LetterTree parent)
        {
            if (strWord.Length == 0)
                return;

            char cLetter = strWord[0];
            
            LetterNode oChild = null;

            if (Children == null)
                Children = new Dictionary<char,LetterNode>();

            if (!Children.ContainsKey(cLetter))
            {
                oChild = new LetterNode(cLetter);
                parent.LetterNodes.Add(oChild);
                Children.Add(cLetter, oChild);
            }
            else
                oChild = Children[cLetter];

            if (strWord.Length == 1)
                oChild.IsCompleteWord = true;

            oChild.AddWord(strWord.Substring(1), parent);
        }

        public IEnumerable<string> Search(string strSearch, int iDepth)
        {
            StoreMaxDepth(iDepth);

            if (IsTerminator && Letter == strSearch[0] && !this.WordEnumerated)
            {
                this.WordEnumerated = true;
                yield return strSearch[0].ToString();
            }

            if (Letter != strSearch[0] && this.Letter != char.MaxValue)
                yield break;

            if(Letter == strSearch[0] && strSearch.Length == 1 && this.IsCompleteWord == true && !this.WordEnumerated)
            {
                this.WordEnumerated = true;
                yield return strSearch[0].ToString();
            }

            string strNewSearch = strSearch;

            if (this.Letter != char.MaxValue)
                strNewSearch = strSearch.Substring(1);

            if (strNewSearch.Length == 0)
                yield break;

            if (IsTerminator)
                yield break;

            if (Children == null || !Children.ContainsKey(strNewSearch[0]))
                yield break;

            foreach (var strSubword in Children[strNewSearch[0]].Search(strNewSearch, iDepth + 1))
                yield return this.Letter + strSubword;
        }

        internal void ResetEnumeration()
        {
            MaxDepthSearched = 0;
            this.WordEnumerated = false;

            if (Children != null)
            {
                foreach (var child in Children.Values)
                {
                    child.ResetEnumeration();
                }
            }
        }        
    }

    public class LetterTree
    {
        public LetterNode TreeRoot = new LetterNode(char.MaxValue);
        public List<LetterNode> LetterNodes = new List<LetterNode>();
        private BlockingCollection<string> m_data = null;

        public void AddWord(string strWord)
        {
            strWord = strWord.ToLower();
            TreeRoot.AddWord(strWord, this);            
        }

        private class ThreadParams
        {
            public string Anagram { get; set; }
            public Action<string> Progress { get; set; }
        }

        private void ResetEnumeration()
        {
            Parallel.ForEach(this.LetterNodes, (node) =>
                {
                    node.ResetEnumeration();
                });
        }
        
        public IEnumerable<string> Search(string strAnagram, Action<string> Progress)
        {
            m_data = new BlockingCollection<string>();

            ResetEnumeration();

            // start the enumeration task
            Task.Factory.StartNew(SearchThread,
                    new ThreadParams()
                    {
                        Anagram = strAnagram,
                        Progress = Progress
                    });

            return m_data.GetConsumingEnumerable();
        }

        private void SearchThread(object threadParam)
        {
            ThreadParams oParams = (ThreadParams)threadParam;

            /// from nuget package https://www.nuget.org/packages/Combinatorics/
            var permutations = new Permutations<char>(oParams.Anagram.ToLower().ToCharArray(), GenerateOption.WithoutRepetition);
            
            // parallel iteration of the permutation list
            Parallel.ForEach(permutations, (IList<char> chars) => {                    
                string strWord = new string(chars.ToArray());

                oParams.Progress?.Invoke(strWord);

                // add each work to the blocking collection
                foreach (var strFoundWord in TreeRoot.Search(strWord, 0))
                    m_data.Add(strFoundWord.Substring(1));
            });

            //Adding complete, the enumeration can stop now
            m_data.CompleteAdding();
        }
    }
}
