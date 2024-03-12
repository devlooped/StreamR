using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Devlooped.AI;

public interface IThreadClient
{
    /// <summary>
    /// Gets the unique identifier for the thread.
    /// </summary>
    string ThreadId { get; }
    /// <summary>
    /// Gets the metadata associated with the thread.
    /// </summary>
    IDictionary<string, string> Metadata { get; }
    /// <summary>
    /// Adds the given content to the thread.
    /// </summary>
    Task AppendAsync(Content content);
    /// <summary>
    /// Deletes the thread and all its contents.
    /// </summary>
    Task DeleteAsync();
    /// <summary>
    /// Replaces the contents of the thread with the given ones.
    /// </summary>
    Task ReplaceAsync(params Content[] contents);
    /// <summary>
    /// Updates any changes to the thread metadata.
    /// </summary>
    Task UpdateAsync();
    /// <summary>
    /// Enumerates the contents of the thread.
    /// </summary>
    IAsyncEnumerable<Content> GetContents(CancellationToken cancellation = default);
}