using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using AnagramSolverLib;
using AnagramSolverSite.Hubs;
using AnagramSolverSite.Services.Interfaces;
using Microsoft.AspNet.SignalR;

namespace AnagramSolverSite.Services
{
    public class CrosswordAnagramProcessService : ICrosswordAnagramProcessService
    {
        class Process
        {
            public Thread thread;
            public DateTime start;
            public long permutationCount;
        }

        private readonly Dictionary<string, int> _wordFrequencies;
        private readonly Dictionary<string, Process> _processes;
        private readonly IHubContext _context;

        public event Action<string, long> OnPermutationCount;
        public event Action<string, double> OnPercentageComplete;
        public event Action<string, string[]> OnSearchComplete;
        public event Action<string, string[]> OnBestGuesses;
        
        public CrosswordAnagramProcessService(Dictionary<string, int> wordFrequencies)
        {
            if (wordFrequencies == null)
                throw new ArgumentNullException("wordFrequencies");
            _wordFrequencies = wordFrequencies;

            _processes = new Dictionary<string, Process>();
            _context = GlobalHost.ConnectionManager.GetHubContext<CrosswordAnagramHub>();
        }

        public void StartNewStearch(string connectionID, string clue, string availableLetters)
        {
            StopOngoingSearch(connectionID);

            var thread = new Thread(new ParameterizedThreadStart(SearchThread));

            _processes.Add(connectionID, new Process() { thread = thread });

            thread.Start(new SearchParameters(connectionID, clue, availableLetters));
        }

        public void StopOngoingSearch(string connectionId)
        {
            if (_processes.ContainsKey(connectionId))
            {
                var process = _processes[connectionId];
                var thread = process.thread;

                if (thread.IsAlive)
                {
                    thread.Abort();
                    thread.Join();
                }

                _processes.Remove(connectionId);
            }
        }

        private void SearchThread(object paramPack)
        {            
            var searchParameters = (SearchParameters)paramPack;
            var clientId = searchParameters.connectionID;
            var search = new CrosswordAnagramService();
            
            _context.Clients.Client(clientId).OnSearchStarted();
            
            search.OnPermutationCount += search_OnPermutationCount;
            search.OnPercentageComplete += search_OnPercentageComplete;
            search.OnBestGuesses += search_OnBestGuesses;
            search.OnSearchComplete += search_OnSearchComplete;

            _processes[clientId].start = DateTime.Now;
            search.CrosswordAnagramSolver(clientId, searchParameters.clue, searchParameters.availableLetters, _wordFrequencies);
        }

        void search_OnPermutationCount(string clientId, long count)
        {
            _processes[clientId].permutationCount = count;
            _context.Clients.Client(clientId).OnPermutationCount(count);
        }        

        void search_OnPercentageComplete(string clientId, double percentComplete)
        {
            var process = _processes[clientId];
            var start = process.start;
            var elapsedTime = DateTime.Now - start;
            var expectedFinish = new TimeSpan(0, 0, 0, 0, (int)((elapsedTime.TotalMilliseconds * 1.0 / (percentComplete / 100.0)) - elapsedTime.TotalMilliseconds));

            _context.Clients.Client(clientId).OnPercentageComplete(percentComplete.ToString("n0"), expectedFinish.ToString());
        }

        void search_OnBestGuesses(string clientId, string[] bestGuesses)
        {
            _context.Clients.Client(clientId).OnBestGuesses(bestGuesses);
        }

        void search_OnSearchComplete(string clientId, string[] topAnswers)
        {
            _context.Clients.Client(clientId).OnSearchComplete(topAnswers);
        }
    }
}