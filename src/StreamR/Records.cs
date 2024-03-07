using System;
using System.Collections.Generic;

namespace Devlooped.AI;

public record Entity(string Id, IDictionary<string, string> Metadata);

public record Thread(string Id, IDictionary<string, string> Metadata) : Entity(Id, Metadata);

public record Content(string Type) : Entity(Guid.NewGuid().ToString(), new Dictionary<string, string>());

public record MessageContent(string Message, string Role = "User") : Content(Role);

public record FileContent(string Path, string Type) : Content(Type)
{
    protected FileContent(string Path) : this(Path, "File") { }
}

public record ImageContent(string Path) : FileContent(Path, "Image");
