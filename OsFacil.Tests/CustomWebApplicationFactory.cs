using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.Extensions.Configuration;
using Moq;
using OsFacil.Data;
using OsFacil.Messaging;
using System;

namespace OsFacil.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
     
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
           
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

           
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("OsFacilTestDb"));

            
            var rabbitDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(RabbitMqProducer));

            if (rabbitDescriptor != null)
                services.Remove(rabbitDescriptor);

           
            var rabbitMock = new Mock<RabbitMqProducer>(new Mock<IConfiguration>().Object);

            
            services.AddSingleton(rabbitMock.Object);
        });
    }
}