using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ReReviewer.IntegrationTests.Infrastructure;

// Настоящее приложение (Program.CreateHostBuilder + Startup) в памяти, без сетевого порта.
// Строка подключения и ключ JWT подменяются, поэтому user-secrets разработчика тестам не нужны.
public sealed class ApiFactory(string connectionString, string environment) : WebApplicationFactory<ReReviewer.Program>
{
    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ReReviewerDB"] = connectionString,
            ["Jwt:SigningKey"] = SigningKey
        }));
    }
}