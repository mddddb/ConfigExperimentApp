namespace ConfigExperiment;

#pragma warning disable CS8618 // Options type, values will be populated in Bind/Configure
public class SubstrateAppTokenIssuer : ParsedAuthArtifactIssuerBase
{
    public override IEnumerable<string> AppIdentifierParts
    {
        get
        {
            yield return "Substrate";
            yield return ApplicationId;
        }
    }
    public string ApplicationId { get; set; }
    public TokenValidationOptions SubstrateAppTokenValidationOptions { get; set; }

    public class TokenValidationOptions
    {
        public string Option1 { get; set; }
        public Dictionary<string, string> AdditionalBag { get; set; }
    }
}
#pragma warning restore CS8618