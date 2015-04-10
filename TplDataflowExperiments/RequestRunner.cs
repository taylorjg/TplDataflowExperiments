using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataflowExperiments
{
    public static class RequestRunner
    {
        private const string FailedToPostRequests = "Failed to post all the requests to the transform block";
        private const string FailedToReceiveResponses = "Failed to receive all the responses from the buffer block";

        public static Tuple<Exception, IEnumerable<TResponse>> RunAsyncRequests<TRequest, TResponse>(
            IEnumerable<TRequest> requests,
            Func<TRequest, Task<TResponse>> createTask,
            int maxDegreeOfParallelism = 1)
        {
            Func<Tuple<Exception, IEnumerable<TResponse>>> makeEmptyResult =
                () => Tuple.Create(null as Exception, Enumerable.Empty<TResponse>());

            Func<Exception, Tuple<Exception, IEnumerable<TResponse>>> makeExceptionResult =
                ex => Tuple.Create(ex, Enumerable.Empty<TResponse>());

            Func<IEnumerable<TResponse>, Tuple<Exception, IEnumerable<TResponse>>> makeSuccessResult =
                results => Tuple.Create(null as Exception, results);

            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var transformBlock = new TransformBlock<TRequest, TResponse>(createTask, options);
            var bufferBlock = new BufferBlock<TResponse>();
            transformBlock.LinkTo(bufferBlock);

            if (!requests.Select(request => transformBlock.Post(request)).All(b => b))
            {
                return makeExceptionResult(new InvalidOperationException(FailedToPostRequests));
            }

            try
            {
                transformBlock.Complete();
                transformBlock.Completion.Wait();

                if (bufferBlock.Count == 0) return makeEmptyResult();

                IList<TResponse> results;
                return !bufferBlock.TryReceiveAll(out results)
                    ? makeExceptionResult(new InvalidOperationException(FailedToReceiveResponses))
                    : makeSuccessResult(results);
            }
            catch (AggregateException ae)
            {
                return makeExceptionResult(ae);
            }
        }
    }
}
