using LLMPipelineSamples.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LLMPipelineSamples
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)

                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Add configuration sources if needed
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    // Configure logging if needed


                })
                .ConfigureServices((hostContext, services) =>
                {

                    services.AddLogging();
                    services.AddHttpClient();

                    services.AddHostedService<AgentChatService>();

                });
    }
}
