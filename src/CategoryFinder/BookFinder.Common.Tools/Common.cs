using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;

namespace BookFinder.Tools
{
    public static class Common
    {
        const string Blanks = "                                                                                                     ";

        public static string GetSettings(string key)
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();
            return configuration[key];
        }

        public static string GetInnerExceptionString(Exception ex, int level)
        {
            if (ex == null)
            {
                return "";
            }
            var str = ex.Message;
            if (ex.InnerException != null)
            {
                str += "\n" + Blanks.Substring(0, level) + "- " + GetInnerExceptionString(ex.InnerException, level + 1);
            }
            return str;
        }

        public static string EncodeBase64(string code, string encoding = "utf-8")
        {
            string encode = "";
            byte[] bytes = Encoding.GetEncoding(encoding).GetBytes(code);

            encode = Convert.ToBase64String(bytes);

            return encode;
        }

        public static string DecodeBase64(string code, string encoding = "utf-8")
        {
            string decode = "";
            byte[] bytes = Encoding.GetEncoding(encoding).GetBytes(code);
            decode = Encoding.GetEncoding(encoding).GetString(bytes);
            return decode;
        }

    }
}
