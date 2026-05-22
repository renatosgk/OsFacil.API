using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Microsoft.Extensions.Hosting;
using OsFacil.Data;
using OsFacil.Messaging;
using OsFacil.MongoDB;

namespace OsFacil.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "OsFacil@SuperSecretKey#2026$FIAP!NetCore8",
                ["Jwt:Issuer"] = "OsFacilAPI",
                ["Jwt:Audience"] = "OsFacilClients",
                ["Jwt:ExpiracaoHoras"] = "8"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            
            var dbName = "OsFacilTestDb_" + Guid.NewGuid();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            var rabbitDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(RabbitMqProducer));
            if (rabbitDescriptor != null) services.Remove(rabbitDescriptor);

            var rabbitMock = new Mock<RabbitMqProducer>(new Mock<IConfiguration>().Object);
            services.AddSingleton(rabbitMock.Object);

           
            var hostedDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(RabbitMqConsumer));
            if (hostedDescriptor != null) services.Remove(hostedDescriptor);

            
            var mongoDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IMongoAuditService));
            if (mongoDescriptor != null) services.Remove(mongoDescriptor);

            var mongoMock = new Mock<IMongoAuditService>();
            mongoMock.Setup(m => m.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(),
                It.IsAny<string?>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mongoMock.Setup(m => m.ObterLogsAsync(It.IsAny<string?>(), It.IsAny<int>()))
                .ReturnsAsync(new List<AuditLog>());

            services.AddSingleton(mongoMock.Object);
        });
    }
}
