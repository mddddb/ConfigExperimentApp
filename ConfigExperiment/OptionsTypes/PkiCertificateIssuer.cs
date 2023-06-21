namespace ConfigExperiment;

#pragma warning disable CS8618 // Options type, values will be populated in Bind/Configure
public class PkiCertificateIssuer : ParsedAuthArtifactIssuerBase
{
    public override IEnumerable<string> AppIdentifierParts
    {
        get
        {
            yield return "PKI";
            yield return RootIssuer;
            yield return ClientCertificate.SubjectName;
        }
    }
    public string RootIssuer { get; set; }
    public CertificateInfo ClientCertificate { get; set; }

    public class CertificateInfo
    {
        public string SubjectName { get; set; }
        public string PublicKeyThumbprint { get; set; }
    }
}
#pragma warning restore CS8618