using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;

namespace Devlooped.AI;

public class ThreadStorage(TableConnection table) : IThreadStorage
{
    ITableRepository<TableEntity> threads = TableRepository.Create(table);

    public ThreadStorage(CloudStorageAccount storage) : this(new TableConnection(storage, "Threads")) { }

    public async Task<IThreadClient> CreateAsync(IDictionary<string, string> metadata)
    {
        var partitionKey = ClaimsPrincipal.Current?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Thread";
        var entity = new TableEntity(partitionKey, Guid.NewGuid().ToString("D"));

        foreach (var item in metadata)
            entity[item.Key] = item.Value;

        await threads.PutAsync(entity);

        return new ThreadClient(this, table, partitionKey, entity.RowKey, metadata);
    }

    public async Task DeleteAsync(string threadId)
    {
        var partitionKey = ClaimsPrincipal.Current?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Thread";

        // By deleting the thread/header first, contents become orphaned and can be cleaned up 
        // by a background process, or by a separate call to delete the contents if needed.
        await threads.DeleteAsync(partitionKey, threadId);

        var actions = new List<TableTransactionAction>();
        await foreach (var entity in threads.CreateQuery().Where(x => x.PartitionKey == partitionKey && x.RowKey == threadId))
        {
            actions.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
        }

        if (actions.Count > 0)
        {
            var service = table.StorageAccount.CreateTableServiceClient();
            var client = service.GetTableClient(table.TableName);

            // We cannot have a single transaction with the header because they have different 
            // partition key (since we want to allow enumeration of threads by user).
            await client.SubmitTransactionAsync(actions);
        }
    }

    public async Task<IThreadClient?> GetAsync(string threadId)
    {
        var partitionKey = ClaimsPrincipal.Current?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Thread";
        var entity = await threads.GetAsync(partitionKey, threadId);

        if (entity == null)
            return null;

        var metadata = new Dictionary<string, string>();
        foreach (var item in entity)
        {
            if (item.Key != "PartitionKey" && item.Key != "RowKey" && item.Value is string value)
                metadata[item.Key] = value;
        }

        return new ThreadClient(this, table, partitionKey, entity.RowKey, metadata);
    }

    public async Task UpdateAsync(string threadId, IDictionary<string, string> metadata)
    {
        var partitionKey = ClaimsPrincipal.Current?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Thread";
        var entity = await threads.GetAsync(partitionKey, threadId);

        if (entity == null)
            return;

        foreach (var item in metadata)
            entity[item.Key] = item.Value;

        await threads.PutAsync(entity);
    }

    class ThreadClient(ThreadStorage storage, TableConnection table, string partitionKey, string rowKey, IDictionary<string, string> metadata) : IThreadClient
    {
        JsonSerializerOptions options = new(JsonSerializerOptions.Default)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public string ThreadId => rowKey;
        public IDictionary<string, string> Metadata => metadata;

        public async Task AppendAsync(Content content)
        {
            var entity = new TableEntity(ThreadId, content.Id)
            {
                ["Content"] = JsonSerializer.Serialize(content, options)
            };
            await storage.threads.PutAsync(entity);
        }

        public Task DeleteAsync() => storage.DeleteAsync(ThreadId);

        public async IAsyncEnumerable<Content> GetContents([EnumeratorCancellation] CancellationToken cancellation = default)
        {
            await foreach (var entity in storage.threads.EnumerateAsync(ThreadId, cancellation))
            {
                if (entity["Content"] is string json &&
                    JsonSerializer.Deserialize<Content>(json) is { } content)
                    yield return content;
            }
        }

        public async Task ReplaceAsync(params Content[] contents)
        {
            var actions = new List<TableTransactionAction>();
            await foreach (var entity in storage.threads.CreateQuery().Where(x => x.PartitionKey == partitionKey && x.RowKey == ThreadId))
            {
                actions.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
            }

            foreach (var content in contents)
            {
                actions.Add(new TableTransactionAction(TableTransactionActionType.Add, new TableEntity(ThreadId, content.Id)
                {
                    ["Content"] = JsonSerializer.Serialize(content, options)
                }));
            }

            if (actions.Count > 0)
            {
                var service = table.StorageAccount.CreateTableServiceClient();
                var client = service.GetTableClient(table.TableName);

                // Delete and insert in a single transaction.
                await client.SubmitTransactionAsync(actions);
            }
        }

        public Task UpdateAsync() => storage.UpdateAsync(ThreadId, metadata);
    }
}
