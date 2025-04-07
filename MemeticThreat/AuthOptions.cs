using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MemeticThreatServerAPI
{
    public class AuthOptions
    {
        const string KEY = "mysupersecret_secretsecretsecretkey!123";
        public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
    }
}
