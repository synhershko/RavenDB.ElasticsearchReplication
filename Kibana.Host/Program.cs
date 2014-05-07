using System.IO;

namespace Kibana.Host
{
    using System;
    using Nancy.Hosting.Self;

    class Program
    {
        static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(Bootstrapper.ZipFilePath) || !File.Exists(Bootstrapper.ZipFilePath))
            {
                Console.WriteLine("Unable to find Kibana");
                return;
            }

            var uri =
                new Uri("http://localhost:3579");

            using (var host = new NancyHost(uri))
            {
                host.Start();

                Console.WriteLine("Kibana is now available on " + uri);
                Console.WriteLine("Press any [Enter] to close the host.");
                Console.ReadLine();
            }
        }
    }
}
