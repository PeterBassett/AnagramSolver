using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AnagramSolverSite.Services.Interfaces
{
    public interface ICrosswordAnagramProcessService
    {
        event Action<string, long> OnPermutationCount;
        event Action<string, double> OnPercentageComplete;
        event Action<string, string[]> OnSearchComplete;
        event Action<string, string[]> OnBestGuesses;

        void StartNewStearch(string connectionID, string clue, string availableLetters);
        void StopOngoingSearch(string connectionID);
    }
}