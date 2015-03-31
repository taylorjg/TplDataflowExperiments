using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using FsCheck;
using FsCheck.Fluent;
using NUnit.Framework;

namespace TplDataflowExperiments
{
    [TestFixture]
    class MaxDegreeOfParallelismTests
    {
        [Test]
        public void SynchronousTest()
        {
            const int numRequests = 50;
            var requests = BuildRequests(numRequests);

            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 5
            };

            var transformBlock = new TransformBlock<Tuple<int, int>, Tuple<int, string>>(
                tuple =>
                {
                    var index = tuple.Item1;
                    var ms = tuple.Item2;
                    LogMessage("Entering transformBlock func - index: {0}; ms: {1}", index, ms);
                    System.Threading.Thread.Sleep(ms);
                    LogMessage("Leaving transformBlock func - index: {0}; ms: {1}", index, ms);
                    return Tuple.Create(index, Convert.ToString(ms));
                },
                options);

            var bufferBlock = new BufferBlock<Tuple<int, string>>();
            transformBlock.LinkTo(bufferBlock);

            var postResults = requests.Select(r => transformBlock.Post(r)).ToList();
            Assert.That(postResults, Is.All.Matches<bool>(b => b));

            LogMessage("Calling transformBlock.Complete()");
            transformBlock.Complete();

            LogMessage("Calling transformBlock.Completion.Wait()");
            transformBlock.Completion.Wait();
            LogMessage("After transformBlock.Completion.Wait()");

            IList<Tuple<int, string>> finalResults;
            var tryReceiveAllResult = bufferBlock.TryReceiveAll(out finalResults);
            Assert.That(tryReceiveAllResult, Is.True);

            foreach (var finalResult in finalResults)
            {
                Console.WriteLine("finalResult: index: {0}; ms: {1}", finalResult.Item1, finalResult.Item2);
            }
        }

        [Test]
        public void AsynchronousTest()
        {
            const int numRequests = 50;
            var requests = BuildRequests(numRequests);

            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 5
            };

            var transformBlock = new TransformBlock<Tuple<int, int>, Tuple<int, string>>(
                tuple =>
                {
                    var index1 = tuple.Item1;
                    var ms1 = tuple.Item2;
                    LogMessage("Entering transformBlock func - index: {0}; ms: {1}", index1, ms1);

                    var task = Task.Factory.StartNew(state =>
                    {
                        var tuple2 = (Tuple<int, int>) state;
                        var index2 = tuple2.Item1;
                        var ms2 = tuple2.Item2;
                        LogMessage("Entering task func - index: {0}; ms: {1}", index2, ms2);
                        System.Threading.Thread.Sleep(ms2);
                        LogMessage("Leaving task func - index: {0}; ms: {1}", index2, ms2);
                        return Tuple.Create(index2, Convert.ToString(ms2));
                    }, tuple);

                    LogMessage("Leaving transformBlock func - index: {0}; ms: {1}", index1, ms1);
                    return task;
                },
                options);

            var bufferBlock = new BufferBlock<Tuple<int, string>>();
            transformBlock.LinkTo(bufferBlock);

            var postResults = requests.Select(r => transformBlock.Post(r)).ToList();
            Assert.That(postResults, Is.All.Matches<bool>(b => b));

            LogMessage("Calling transformBlock.Complete()");
            transformBlock.Complete();

            LogMessage("Calling transformBlock.Completion.Wait()");
            transformBlock.Completion.Wait();
            LogMessage("After transformBlock.Completion.Wait()");

            IList<Tuple<int, string>> finalResults;
            var tryReceiveAllResult = bufferBlock.TryReceiveAll(out finalResults);
            Assert.That(tryReceiveAllResult, Is.True);

            foreach (var finalResult in finalResults)
            {
                Console.WriteLine("finalResult: index: {0}; ms: {1}", finalResult.Item1, finalResult.Item2);
            }
        }

        private static IEnumerable<Tuple<int, int>> BuildRequests(int numRequests)
        {
            var g = Any.IntBetween(1, 100).MakeListOfLength(numRequests);
            var sample = Gen.sample(0, 1, g);
            var requests = sample.First();
            return Enumerable.Range(0, int.MaxValue).Zip(requests, Tuple.Create);
        }

        private static void LogMessage(string format, params object[] args)
        {
            var message = string.Format(format, args);
            var line = string.Format("[{0}] {1}", System.Threading.Thread.CurrentThread.ManagedThreadId, message);
            Console.WriteLine(line);
        }
    }
}
