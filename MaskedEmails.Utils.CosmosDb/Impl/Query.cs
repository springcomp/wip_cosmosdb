using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Utils.CosmosDb.Interop;

public sealed class Query<T> : IAsyncEnumerable<Page<T>> where T : ICosmosDbItem
{
    private readonly Container container_;
    private readonly QueryDefinition query_;

    public Query(Container container, string query)
    {
        container_ = container;
        query_ = new QueryDefinition(query);
    }

    public IAsyncEnumerator<Page<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new Enumerator(container_, query_, cancellationToken);
    }

    private sealed class Enumerator : IAsyncEnumerator<Page<T>>
    {
        private readonly Container container_;
        private readonly QueryDefinition query_;
        private FeedIterator<T> iterator_ = null;
        private Page<T> current_ = null;

        public Enumerator(Container container, QueryDefinition query, CancellationToken cancellationToken)
        {
            container_ = container;
            query_ = query;
        }

        public Page<T> Current
            => current_;

        public async ValueTask<bool> MoveNextAsync()
        {
            iterator_ = iterator_ ?? container_.GetItemQueryIterator<T>(query_);
            var result = iterator_.HasMoreResults;
            if (result)
            {
                var response = await iterator_.ReadNextAsync();
                current_ = new Page<T>(response);
            }
            return result;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }
    }
}