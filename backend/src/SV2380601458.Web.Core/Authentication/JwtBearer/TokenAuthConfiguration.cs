using Microsoft.IdentityModel.Tokens;
using System;

namespace SV2380601458.Authentication.JwtBearer
{
    public class TokenAuthConfiguration
    {
        public SymmetricSecurityKey SecurityKey { get; set; }

        public string Issuer { get; set; }

        public string Audience { get; set; }

        public SigningCredentials SigningCredentials { get; set; }

        public TimeSpan Expiration { get; set; }
    }
}
