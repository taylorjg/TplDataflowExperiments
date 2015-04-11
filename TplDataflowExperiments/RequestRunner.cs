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

        public static Tuple<Exception, IEnumerable<TResponse>> RunAsyncRequests<TRequest, TResponse>(
            IEnumerable<TRequest> requests,
            Func<TRequest, Task<TResponse>> createTask,
            int maxDegreeOfParallelism = 1)
        {
            Func<Exception, Tuple<Exception, IEnumerable<TResponse>>> makeExceptionResult =
                ex => Tuple.Create(ex, Enumerable.Empty<TResponse>());

            Func<IEnumerable<TResponse>, Tuple<Exception, IEnumerable<TResponse>>> makeSuccessResult =
                rs => Tuple.Create(null as Exception, rs);

            var responses = new List<TResponse>();

            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var transformBlock = new TransformBlock<TRequest, TResponse>(createTask, options);
            var actionBlock = new ActionBlock<TResponse>(response => responses.Add(response));
            transformBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });

            if (!requests.Select(request => transformBlock.Post(request)).All(Id))
            {
                return makeExceptionResult(new ApplicationException(FailedToPostRequests));
            }

            try
            {
                transformBlock.Complete();
                actionBlock.Completion.Wait();
                return makeSuccessResult(responses);
            }
            catch (AggregateException ae)
            {
                return makeExceptionResult(ae);
            }
        }

        private static T Id<T>(T t)
        {
            return t;
        }
    }
}
