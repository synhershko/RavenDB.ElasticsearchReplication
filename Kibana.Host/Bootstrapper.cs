using System;
using System.IO;
using Nancy.Conventions;
using Nancy.Responses;

namespace Kibana.Host
{
    using Nancy;

    public class Bootstrapper : DefaultNancyBootstrapper
    {
        // The bootstrapper enables you to reconfigure the composition of the framework,
        // by overriding the various methods and properties.
        // For more information https://github.com/NancyFx/Nancy/wiki/Bootstrapper

        protected override void ConfigureConventions(NancyConventions conventions)
        {
            base.ConfigureConventions(conventions);

            conventions.StaticContentsConventions.Clear();
            conventions.StaticContentsConventions.Add((ctx, root) =>
            {
                var reqPath = ctx.Request.Path;

                if (reqPath.Equals("/"))
                {
                    reqPath = "/index.html";
                }

                reqPath = KibanaFileName + reqPath.Replace('\\', '/');

                var fileName = Path.GetFullPath(Path.Combine(root, reqPath));
                if (File.Exists(fileName))
                {
                    return new GenericFileResponse(fileName, ctx);
                }

                return new SpecialEmbeddedFileResponse(
                    GetType().Assembly,
                    ZipFilePath,
                    reqPath,
                    ctx.Request.Headers);
            });
        }

        public static string ZipFilePath { get; private set; }
        private const string KibanaFileName = "kibana-3.0.1";

        static Bootstrapper()
        {
            var fullZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, KibanaFileName + ".zip");
            if (File.Exists(fullZipPath) == false)
                fullZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", KibanaFileName + ".zip");
            ZipFilePath = fullZipPath;
        }
    }
}