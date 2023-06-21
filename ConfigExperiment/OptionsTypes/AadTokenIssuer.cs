namespace ConfigExperiment;

#pragma warning disable CS8618 // Options type, values will be populated in Bind/Configure
public class AadTokenIssuer : ParsedAuthArtifactIssuerBase
{
    public override IEnumerable<string> AppIdentifierParts
    {
        get
        {
            yield return "AAD";
            yield return AadInstance;
            yield return ApplicationId.ToString();
        }
    }

    public string AadInstance { get; set; }
    public Guid ApplicationId { get; set; }

    public List<string> Audiences { get; set; }
    public List<string> AcceptedTokenVersions { get; set; }
}
#pragma warning restore CS8618