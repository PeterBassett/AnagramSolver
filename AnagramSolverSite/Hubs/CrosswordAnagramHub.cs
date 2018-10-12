using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AnagramSolverLib;
using AnagramSolverSite.Services.Interfaces;
using Microsoft.AspNet.SignalR;

namespace AnagramSolverSite.Hubs
{
    public class CrosswordAnagramHub : Hub
    {
        private readonly ICrosswordAnagramProcessService _processService;

        public CrosswordAnagramHub(ICrosswordAnagramProcessService processService)
        {
            if (processService == null)
                throw new ArgumentNullException("processService");
            _processService = processService;
        }

        public void Search(string clue, string availableLetters)
        {
            _processService.StartNewStearch(Context.ConnectionId, clue, availableLetters);            
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            _processService.StopOngoingSearch(Context.ConnectionId);                        
            return base.OnDisconnected(stopCalled);
        }
    }
}