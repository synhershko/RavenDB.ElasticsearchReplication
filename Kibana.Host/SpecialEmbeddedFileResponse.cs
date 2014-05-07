using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using Nancy;

namespace Kibana.Host
{
    public class SpecialEmbeddedFileResponse : Response
    {
        private readonly bool _disableRequestCompression;

        public SpecialEmbeddedFileResponse(Assembly assembly, string zipFilePath, string resourcePath, RequestHeaders requestHeaders = null, bool disableRequestCompression = false)
        {
            _disableRequestCompression = disableRequestCompression;

            // Generate the etag for the zip file and use it for optionally returning HTTP Not-Modified
            var zipFileEtag = "zip" + File.GetLastWriteTime(zipFilePath).Ticks.ToString("G");
            if (requestHeaders != null && (requestHeaders.IfMatch.Any(x => x == zipFileEtag) || requestHeaders.IfNoneMatch.Any(x => x == zipFileEtag)))
            {
                StatusCode = HttpStatusCode.NotModified;
                this.WithHeader("ETag", zipFileEtag);
                return;
            }

            var content = GetFileFromZip(zipFilePath, resourcePath);
            if (content != null)
            {
                Contents = content;
                if (_disableRequestCompression == false)
                    Headers["Content-Encoding"] = "gzip";
                this.WithHeader("ETag", zipFileEtag);
            }
            else
            {
                // Potentially fall back to loading the requested file if it was embedded as a resource
                var fileContent = assembly.GetManifestResourceStream(resourcePath);
                if (fileContent == null)
                {
                    StatusCode = HttpStatusCode.NotFound;
                    return;
                }
                Contents = GetFileContent(fileContent);
            }
            
            ContentType = MimeTypes.GetMimeType(Path.GetFileName(resourcePath));
            StatusCode = HttpStatusCode.OK;
        }

        private Action<Stream> GetFileFromZip(string zipPath, string docPath)
        {
            var fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var zipFile = new ZipFile(fileStream);
            var zipEntry = zipFile.GetEntry(docPath);

            if (zipEntry == null || zipEntry.IsFile == false)
                return null;

            var data = zipFile.GetInputStream(zipEntry);
            if (data == null) return null;

            return stream =>
                   {
                       try
                       {
                           if (_disableRequestCompression == false)
                               stream = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true);

                           data.CopyTo(stream);
                           stream.Flush();
                       }
                       finally
                       {
                           if (_disableRequestCompression == false)
                               stream.Dispose();
                       }
                   };
        }

        private static Action<Stream> GetFileContent(Stream content)
        {
            return stream =>
            {
                using (content)
                {
                    content.Seek(0, SeekOrigin.Begin);
                    content.CopyTo(stream);
                }
            };
        }
    }
}
