using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Devlooped.AI;


public class ThreadManager : IThreadStorage
{
    public Task<IThreadClient> CreateAsync(IDictionary<string, string> metadata) => throw new NotImplementedException();
    public Task DeleteAsync(string threadId) => throw new NotImplementedException();
    public Task<IThreadClient> GetAsync(string threadId) => throw new NotImplementedException();
}
