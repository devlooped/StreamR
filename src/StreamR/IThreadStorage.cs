using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devlooped.AI;

public interface IThreadStorage
{
    Task<IThreadClient> CreateAsync(IDictionary<string, string> metadata);
    Task DeleteAsync(string threadId);
    Task<IThreadClient> GetAsync(string threadId);
}