using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Fluent;
using NUnit.Framework;

namespace TplDataflowExperiments
{
    [TestFixture]
    public class RequestRunnerTests
    {
        [Test]
        public void Test()
        {
            var requests = BuildRequests(50);
            var responses = RequestRunner.RunAsyncRequests(requests, CreateTask, 5);
            foreach (var response in responses)
            {
                Console.WriteLine("response: index: {0}; ms: {1}", response.Item1, response.Item2);
            }
        }

        private static IEnumerable<Tuple<int, int>> BuildRequests(int numRequests)
        {
            var g = Any.IntBetween(1, 100).MakeListOfLength(numRequests);
            var sample = Gen.sample(0, 1, g);
            var requests = sample.First();
            return Enumerable.Range(0, int.MaxValue).Zip(requests, Tuple.Create);
        }

        private static Task<Tuple<int, string>> CreateTask(Tuple<int, int> request)
        {
            return Task.Factory.StartNew(
                state =>
                {
                    var tuple = (Tuple<int, int>) state;
                    var index = tuple.Item1;
                    var ms = tuple.Item2;
                    return Tuple.Create(index, Convert.ToString(ms));
                },
                request);
        }
    }
}
