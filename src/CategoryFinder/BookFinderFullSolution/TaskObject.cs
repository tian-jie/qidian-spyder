using BookFinder.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BookFinderFullSolution
{
    public class TaskObject
    {
        public Task Task { get; set; }

        public CancellationTokenSource CancellationToken { get; set; }

    }

}
