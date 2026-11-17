using System.Net;
using System.Net.Http.Headers;

namespace MiniCRM.IntegrationTests;

public sealed class AuthorizationTests(CrmApiFactory factory) : IClassFixture<CrmApiFactory>
{
    [Fact]
    public async Task Crm_routes_require_a_valid_access_token()
    {
        using var client=factory.CreateClient();
        var response=await client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }

    [Fact]
    public async Task Valid_sales_token_can_read_dashboard_aggregate()
    {
        using var client=factory.CreateClient();
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",CrmApiFactory.CreateToken());
        var response=await client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var json=await response.Content.ReadAsStringAsync();
        Assert.Contains("12500",json,StringComparison.Ordinal);
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        using var client=factory.CreateClient();
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",CrmApiFactory.CreateToken(expired:true));
        var response=await client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }

    [Fact]
    public async Task Sales_representative_cannot_open_admin_user_management()
    {
        using var client=factory.CreateClient();
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",CrmApiFactory.CreateToken("SalesRepresentative"));
        var response=await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);
    }

    [Fact]
    public async Task Invalid_customer_write_is_rejected_before_service_call()
    {
        using var client=factory.CreateClient();
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",CrmApiFactory.CreateToken());
        using var body=new StringContent("{\"firstName\":\"\",\"lastName\":\"\",\"email\":\"not-an-email\"}",System.Text.Encoding.UTF8,"application/json");
        var response=await client.PostAsync("/api/customers",body);
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
    }
}
