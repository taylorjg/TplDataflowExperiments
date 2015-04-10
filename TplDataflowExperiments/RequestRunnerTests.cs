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
        public void SomeRequests()
        {
            var requests = BuildRequests(50);
            var responses = RequestRunner.RunAsyncRequests(requests, CreateTask, 5);
            Assert.That(responses.Item1, Is.Null);
            Assert.That(responses.Item2, Is.Not.Empty);
            foreach (var response in responses.Item2)
            {
                Console.WriteLine("response: index: {0}; ms: {1}", response.Item1, response.Item2);
            }
        }

        [Test]
        public void NoRequests()
        {
            var requests = Enumerable.Empty<Tuple<int, int>>();
            var responses = RequestRunner.RunAsyncRequests(requests, CreateTask, 5);
            Assert.That(responses.Item1, Is.Null);
            Assert.That(responses.Item2, Is.Empty);
        }

        [Test]
        public void TaskThrowsException()
        {
            var requests = BuildRequests(50);

            var responses = RequestRunner.RunAsyncRequests(
                requests,
                request =>
                {
                    if (request.Item1 == 13) throw new DivideByZeroException();
                    return CreateTask(request);
                },
                5);

            Assert.That(responses.Item1, Is.Not.Null);
            Assert.That(responses.Item2, Is.Empty);
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
                    Console.WriteLine("Inside task: index: {0}; ms: {1}", index, ms);
                    return Tuple.Create(index, Convert.ToString(ms));
                },
                request);
        }
    }
}
