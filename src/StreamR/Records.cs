using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Devlooped.AI;

public record Entity(string Id, IDictionary<string, string>? Metadata = default);

public record Thread(string Id, IDictionary<string, string>? Metadata = default) : Entity(Id, Metadata);

[JsonDerivedType<MessageContent>]
[JsonDerivedType<FileContent>]
public abstract record Content(string Type) : Entity(Guid.NewGuid().ToString());

public record MessageContent(string Message, string Role = "User") : Content(Role);

public record FileContent(string Path, string Type) : Content(Type)
{
    protected FileContent(string Path) : this(Path, "File") { }
}

//public record ImageContent(string Path) : FileContent(Path, "Image");

public class JsonDerivedTypeAttribute<T> : JsonDerivedTypeAttribute
{
    public JsonDerivedTypeAttribute(string? typeDiscriminator = default) : base(typeof(T), typeDiscriminator ?? typeof(T).Name)
    {
    }
}