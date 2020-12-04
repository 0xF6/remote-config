namespace ivy
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    public static class Program
    {
        public static void Main(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(Configure)
                .Build()
                .Run();

        public static void Configure(IWebHostBuilder webBuilder)
        {
            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            webBuilder
                .UseStartup<Startup>()
                .UseUrls($"http://*.*.*.*:{port}");
        }
    }
}