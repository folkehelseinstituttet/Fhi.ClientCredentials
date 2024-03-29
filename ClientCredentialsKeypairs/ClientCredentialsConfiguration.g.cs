﻿// Autogenerated from json from setup of key in NHN Selvbetjening, using paste special as C#

#nullable disable
namespace Fhi.ClientCredentialsKeypairs;

public partial class ClientCredentialsConfiguration
{
    public string clientName { get; set; }
    public string authority { get; set; }
    public string clientId { get; set; }
    public string[] grantTypes { get; set; }
    public string[] scopes { get; set; }
    public string secretType { get; set; }
    public string rsaPrivateKey { get; set; }
    public int rsaKeySizeBits { get; set; }
    public string privateJwk { get; set; }
}