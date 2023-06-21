using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;

namespace ConfigExperiment.Controllers;

[ApiController]
[Route("/options")]
public class OptionsValueController : ControllerBase
{
    [HttpGet]
    public object Get(
        [FromServices] IOptions<AppRegistryEntryOptions> options,
        [FromServices] IOptionsMonitor<AppRegistryEntryOptions> optionsMonitor,
        [FromServices] IOptionsSnapshot<AppRegistryEntryOptions> optionsSnapshot
        )
    {
        return new
        {
            options = options.Value,
            optionsMonitor = optionsMonitor.CurrentValue,
            optionsSnapshot = optionsSnapshot.Value
        };
    }
}