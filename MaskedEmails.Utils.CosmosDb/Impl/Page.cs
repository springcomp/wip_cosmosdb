using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using Utils.CosmosDb.Interop;
using System.Collections;

public sealed class Page<T> : IEnumerable<T> where T : ICosmosDbItem
{
    private readonly FeedResponse<T> response_;

    internal Page(FeedResponse<T> response)
    {
        response_ = response;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return response_.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (this as IEnumerable<T>).GetEnumerator();
    }
}