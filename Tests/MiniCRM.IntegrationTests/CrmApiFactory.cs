using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MiniCRM.Application;
using Moq;

namespace MiniCRM.IntegrationTests;

public sealed class CrmApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "local-integration-test-signing-key-at-least-32-bytes";
    public CrmApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "MiniCRM");
        Environment.SetEnvironmentVariable("Jwt__Audience", "MiniCRM.React");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string,string?>
        { ["Jwt:SigningKey"]=SigningKey, ["Jwt:Issuer"]="MiniCRM", ["Jwt:Audience"]="MiniCRM.React", ["FrontendOrigin"]="http://localhost:5173" }));
        builder.ConfigureLogging(logging=>logging.ClearProviders());
        builder.ConfigureTestServices(services=>
        {
            var crm=new Mock<ICrmService>();
            crm.Setup(x=>x.DashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new DashboardSummary(8,4,1,2,1,1,12500m,7500m,3,1));
            services.RemoveAll<ICrmService>();services.AddSingleton(crm.Object);
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,options=>options.Events.OnTokenValidated=_=>Task.CompletedTask);
        });
    }
    public static string CreateToken(string role="SalesRepresentative", bool expired=false)
    {
        var credentials=new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),SecurityAlgorithms.HmacSha256);
        var claims=new[]{new Claim(ClaimTypes.NameIdentifier,"41"),new Claim(JwtRegisteredClaimNames.Sub,"41"),new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString("N")),new Claim("minicrm:security_stamp","test-stamp"),new Claim(ClaimTypes.Role,role)};
        var now=DateTime.UtcNow;return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("MiniCRM","MiniCRM.React",claims,now.AddMinutes(-10),expired?now.AddMinutes(-2):now.AddMinutes(10),credentials));
    }
}
