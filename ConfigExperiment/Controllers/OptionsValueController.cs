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
        [FromServices] IOptionsMonitor<SingleInstanceOptions> singleInstanceOptions,
        [FromServices] IOptionsMonitor<ListOptions> listOptions,
        [FromServices] IOptions<DictionaryOptions> dictOptions,
        [FromServices] IOptions<DictionaryOptions_KeyIsTypeIdentifier> dictOptions_KeyIsTypeIdentifier
        )
    {
        return new
        {
            singleInstanceOptions_1 = singleInstanceOptions.Get("1"),
            singleInstanceOptions_2 = singleInstanceOptions.Get("2"),
            singleInstanceOptions_2_withoutConfigSectionProperty = singleInstanceOptions.Get("2_withoutConfigSectionProperty"),
            listOptions = listOptions.CurrentValue,
            listOptions_withoutConfigSectionProperty = listOptions.Get("withoutConfigSectionProperty"),
            dictOptions = dictOptions.Value,
            dictOptions_KeyIsTypeIdentifier = dictOptions_KeyIsTypeIdentifier.Value
        };
    }
}