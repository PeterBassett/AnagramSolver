using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnagramSolverSite.Services
{
    class SearchParameters
    {
        public string clue;
        public string availableLetters;
        public string connectionID;
        
        public SearchParameters(string connectionID, string clue, string availableLetters)
        {
            this.connectionID = connectionID;
            this.clue = clue.ToLower().Trim();
            this.availableLetters = availableLetters.ToLower().Trim();
        }           
    }
}
