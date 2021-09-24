using Microsoft.Extensions.Configuration;
using System.IO;

namespace CategoryFinder
{
    public static class Common
    {
        public static string GetSettings(string key)
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();
            return configuration[key];
        }
    }
}
