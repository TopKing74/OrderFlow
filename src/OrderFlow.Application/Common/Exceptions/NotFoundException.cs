namespace OrderFlow.Application.Common.Exceptions;

public sealed class NotFoundException(string name, object key)
    : Exception($"{name} with key '{key}' was not found.")
{
    public string Name { get; } = name;

    public object Key { get; } = key;
}
