using Microsoft.Extensions.Configuration;

namespace CustomConfigurationBinder;

public interface ICustomConfigurationBinder
{
    object? Bind(string typeIdentifierKey, IConfiguration config);
}
