using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataflowExperiments
{
    public static class RequestRunner
    {
        public static IEnumerable<TResponse> RunAsyncRequests<TRequest, TResponse>(
            IEnumerable<TRequest> requests,
            Func<TRequest, Task<TResponse>> createTask,
            int maxDegreeOfParallelism = 1)
        {
            // TODO: add error handling

            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var transformBlock = new TransformBlock<TRequest, TResponse>(createTask, options);
            var bufferBlock = new BufferBlock<TResponse>();
            transformBlock.LinkTo(bufferBlock);
            foreach (var request in requests) transformBlock.Post(request);
            transformBlock.Complete();
            transformBlock.Completion.Wait();
            if (bufferBlock.Count == 0) return Enumerable.Empty<TResponse>();
            IList<TResponse> results;
            bufferBlock.TryReceiveAll(out results);
            return results;
        }
    }
}
