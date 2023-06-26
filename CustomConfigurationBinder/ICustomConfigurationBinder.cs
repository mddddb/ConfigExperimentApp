using Microsoft.Extensions.Configuration;

namespace CustomConfigurationBinder;

public interface ICustomConfigurationBinder
{
    string TypeIdentifierKey { get; }
    void Bind(IConfigurationSection configSection, ref object? options);
}
