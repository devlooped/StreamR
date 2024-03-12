using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devlooped.AI;

public class TableStorageTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreateAndSave()
    {
        var manager = new ThreadStorage(CloudStorageAccount.DevelopmentStorageAccount);
        var metadata = new Dictionary<string, string>
        {
            ["Name"] = "TestThread",
            ["Description"] = "Test description"
        };

        var thread = await manager.CreateAsync(metadata);

        Assert.NotNull(thread);
        Assert.Equal(metadata["Name"], thread.Metadata["Name"]);
        Assert.Equal(metadata["Description"], thread.Metadata["Description"]);

        thread.Metadata["Name"] = "UpdatedName";
        thread.Metadata["New"] = "NewValue";

        await thread.UpdateAsync();

        var saved = await manager.GetAsync(thread.ThreadId);
        Assert.NotNull(saved);
        Assert.Equal("UpdatedName", saved.Metadata["Name"]);
        Assert.Equal("NewValue", saved.Metadata["New"]);

        await thread.AppendAsync(new MessageContent("Hello world"));
        await thread.AppendAsync(new FileContent("file.txt", "text/plain"));

        MessageContent? message = null;
        FileContent? file = null;

        await foreach (var content in thread.GetContents())
        {
            Assert.NotNull(content);

            if (content is MessageContent m)
                message = m;
            else if (content is FileContent f)
                file = f;
            else
                Assert.Fail("Unexpected content type");
        }

        Assert.Equal("Hello world", message?.Message);
        Assert.Equal("file.txt", file?.Path);

        await manager.DeleteAsync(thread.ThreadId);

        var deleted = await manager.GetAsync(thread.ThreadId);

        Assert.Null(deleted);
    }
}
