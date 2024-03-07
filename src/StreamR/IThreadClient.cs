using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Devlooped.AI;

public interface IThreadClient
{
    string ThreadId { get; }
    Task AppendAsync(Content content);
    Task DeleteAsync();
    Task ReplaceAsync(params Content[] contents);
    Task UpdateAsync(IDictionary<string, string> metadata);
    IAsyncEnumerable<Content> GetContents(CancellationToken cancellation = default);
}