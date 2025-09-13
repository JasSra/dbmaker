namespace DbMaker.API.Authentication;

public class IdpConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public string? MetaDataEndpoint { get; set; }
    public bool IsAzureAdB2C { get; set; }
    public string[]? Scopes { get; set; }
    public Dictionary<string, string>? RoleMappings { get; set; }
    
    public static IdpConfiguration FromAzureAdB2C(string name, string instance, string tenantId, string clientId, string policyId)
    {
        return new IdpConfiguration
        {
            Name = name,
            Issuer = $"{instance.TrimEnd('/')}/{tenantId}/v2.0/",
            Audience = clientId,
            SigningKey = string.Empty, // Not needed for B2C token validation
            //MetaDataEndpoint = $"{instance.TrimEnd('/')}/{tenantId}/{policyId}/v2.0/.well-known/openid_configuration",
            MetaDataEndpoint = $"{instance.TrimEnd('/')}/{tenantId}/v2.0/.well-known/openid-configuration?p={policyId}",
            IsAzureAdB2C = true
        };
    }//https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/v2.0/.well-known/openid-configuration?p=B2C_1_SIGNUP_SIGNIN
}

public class JwksResponse
{
    public List<JwksKey>? Keys { get; set; }
}

public class JwksKey
{
    public string? Kid { get; set; }
    public string? Kty { get; set; }
    public string? Use { get; set; }
    public string? N { get; set; }
    public string? E { get; set; }
    public string[]? X5c { get; set; }
    public string? X5t { get; set; }

    public Microsoft.IdentityModel.Tokens.SecurityKey ToSecurityKey()
    {
        if (Kty == "RSA" && !string.IsNullOrEmpty(N) && !string.IsNullOrEmpty(E))
        {
            var rsa = new System.Security.Cryptography.RSACryptoServiceProvider();
            rsa.ImportParameters(new System.Security.Cryptography.RSAParameters
            {
                Modulus = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(N),
                Exponent = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(E)
            });
            
            return new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa) { KeyId = Kid };
        }

        throw new NotSupportedException($"Key type {Kty} is not supported");
    }
}