using System.Collections.Generic;
using System.Threading;

namespace Devlooped.AI;

public abstract class RunSettings(string threadId)
{
    public string ThreadId => threadId;
}

public class ModelRunSettings(string modelName, string threadId) : RunSettings(threadId)
{
    public string ModelName => modelName;
}

public interface IThreadRunner
{
    // as opposed to RunAsync eventually
    IAsyncEnumerable<string> StreamAsync(RunSettings settings, CancellationToken cancellation = default);
}
