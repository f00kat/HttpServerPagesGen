using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServerPagesGen
{
    class FSDataCreator
    {
        class FileDescriptor
        {
            private readonly string _file;

            private static Dictionary<string, string> _supportedExtHTTPContentType = new Dictionary<string, string> {
             { "html",  "text/html"},
             { "htm",   "text/html" },
             { "shtml", "text/html\r\nExpires: Fri, 10 Apr 2008 14:00:00 GMT\r\nPragma: no-cache"},
             { "shtm",  "text/html\r\nExpires: Fri, 10 Apr 2008 14:00:00 GMT\r\nPragma: no-cache"},
             { "ssi",   "text/html\r\nExpires: Fri, 10 Apr 2008 14:00:00 GMT\r\nPragma: no-cache"},
             { "gif",   "image/gif"},
             { "png",   "image/png"},
             { "jpg",   "image/jpeg"},
             { "bmp",   "image/bmp"},
             { "ico",   "image/x-icon"},
             { "class", "application/octet-stream"},
             { "cls",   "application/octet-stream"},
             { "js",    "application/x-javascript"},
             { "ram",   "application/x-javascript"},
             { "css",   "text/css"},
             { "swf",   "application/x-shockwave-flash"},
             { "xml",   "text/xml"}
            };

            public FileDescriptor(string file)
            {
                _file = file;
            }

            public string FileName
            {
                get
                {
                    return _file;
                }
            }

            public string ServerFileName
            {
                get
                {
                    return _file.Replace(DEFAULT_DIR, String.Empty)
                                .Replace("\\", "/");
                }
            }

            public string FileNameInCFormat
            {
                get
                {
                    return _file.Replace(DEFAULT_DIR + "\\", String.Empty)
                                .Replace(".", "_")
                                .Replace("\\", "_");
                }
            }

            public string FileExtension
            {
                get
                {
                    return Path.GetExtension(_file)
                               .Replace(".", String.Empty);
                }
            }

            public string ServerFileNameInHEX
            {
                get
                {
                    var result = new StringBuilder();
                    ASCIIResult fileName = ToHEXFormat($"{ServerFileName}\0");

                    result.Append($"/* {ServerFileName} ({fileName.Size} bytes) */ {newline}");
                    result.Append(fileName.ASCIIDataWithComma);

                    return result.ToString();
                }
            }

            public int ServerFileNameLen
            {
                get
                {
                    return ToHEXFormat($"{ServerFileName}\0").Size;
                }
            }

            public string HTTPContentType
            {
                get
                {
                    var result = new StringBuilder();

                    var contentType = GetHTTPContentType(FileExtension);
                    ASCIIResult contentTypeFull = ToHEXFormat($"Content-type: {contentType}{newline}{newline}");

                    result.Append($"/* Content-type: {contentType} ({contentTypeFull.Size} bytes)*/{newline}");
                    result.Append(contentTypeFull.ASCIIDataWithComma);

                    return result.ToString();
                }
            }

            private static string GetHTTPContentType(string ext)
            {
                string contentType;
                if (_supportedExtHTTPContentType.TryGetValue(ext, out contentType))
                    return contentType;

                return DEFAULT_HTTP_CONTENT_TYPE;
            }

            public string HTTPData
            {
                get
                {
                    var result = new StringBuilder();
                    var fileData = File.ReadAllBytes(FileName);
                    ASCIIResult data = ToHEXFormat(fileData);
                    result.Append($"/* RAW Data ({data.Size} bytes)*/ {newline}");
                    result.Append(data.ASCIIData);

                    return result.ToString();
                }
            }
        }

        class ASCIIResult
        {
            public ASCIIResult(string data, int size)
            {
                Size = size;
                ASCIIData = data;
            }

            public string ASCIIData { get; private set; }
            public int Size { get; private set; }

            public string ASCIIDataWithComma
            {
                get
                {
                    return !String.IsNullOrEmpty(ASCIIData) ? ASCIIData + "," : ASCIIData;
                }
            }
        }

        private static string newline = Environment.NewLine;

        private static string DEFAULT_HTTP_CONTENT_TYPE = "text/plain";

        private const string DEFAULT_DIR = "fs";

        private const int BYTES_PER_LINE = 16;

        private static string SERVER_FOOT_PRINT = "lwIP/1.3.2";

        private static string LastModifiedDate = DateTime.Now.ToString("r");

        public FSDataCreator()
        {

        }

        public string Create()
        {
            var result = new StringBuilder();

            /* C header */
            result.Append(GenerateCHeader());

            var files = new LinkedList<FileDescriptor>();
            GetFiles(DEFAULT_DIR, files);

            /* Data section */
            result.Append(GenerateData(files));

            /* Struct section */
            result.Append(GenerateFiles(files));

            return result.ToString();
        }

        private string GenerateCHeader()
        {
            var result = new StringBuilder();

            result.Append($"#include \"fs.h\" {newline}");
            result.Append($"#include \"lwip/def.h\" {newline}");
            result.Append($"#include \"fsdata.h\" {newline}");

            result.Append(newline);

            result.Append($"#define file_NULL (struct fsdata_file *) NULL {newline}");

            result.Append(newline);

            Console.WriteLine("Header generated");

            return result.ToString();
        }

        private string GenerateData(IEnumerable<FileDescriptor> files)
        {
            var result = new StringBuilder();
            int i = 0;
            foreach (var file in files)
            {
                result.Append($"static const unsigned int dummy_align__{file.FileNameInCFormat} = {i};{newline}");
                result.Append($"static const unsigned char data__{file.FileNameInCFormat}[] = {{ {newline}");

                /* File name */
                result.Append(file.ServerFileNameInHEX);
                result.Append(newline);

                /* HTTP Header */
                result.Append(GetHTTPHeader());
                result.Append(newline);

                /* Last-Modified */
                result.Append(GetLastModified());
                result.Append(newline);

                /* Server */
                result.Append(GetServerFootPrint());
                result.Append(newline);

                /* Content-type */
                result.Append(file.HTTPContentType);
                result.Append(newline);
                result.Append(newline);


                /* Data */
                result.Append(file.HTTPData);

                result.Append("};");

                result.Append(newline);
                result.Append(newline);

                Console.WriteLine($"file data '{file.FileNameInCFormat}' generated");

                i++;
            }

            return result.ToString();
        }

        private static string GetHTTPHeader()
        {
            var result = new StringBuilder();
            ASCIIResult header = ToHEXFormat($"HTTP/1.0 200 OK{newline}");
            result.Append($"/* HTTP/1.0 200 OK ({header.Size} bytes)*/{newline}");
            result.Append(header.ASCIIDataWithComma);

            return result.ToString();
        }

        private static string GetLastModified()
        {
            var result = new StringBuilder();
            ASCIIResult lastModified = ToHEXFormat($"Last-Modified: {LastModifiedDate}{newline}");
            result.Append($"/* Last-Modified: {LastModifiedDate} ({lastModified.Size} bytes) */{newline}");
            result.Append(lastModified.ASCIIDataWithComma);

            return result.ToString();
        }

        private static string GetServerFootPrint()
        {
            var result = new StringBuilder();
            ASCIIResult server = ToHEXFormat($"Server: {SERVER_FOOT_PRINT}{newline}");
            result.Append($"/* Server: {SERVER_FOOT_PRINT} ({server.Size} bytes) */{newline}");
            result.Append(server.ASCIIDataWithComma);

            return result.ToString();
        }

        private string GenerateFiles(LinkedList<FileDescriptor> files)
        {
            var result = new StringBuilder();

            /*

            struct fsdata_file {
                const struct fsdata_file *next;
                const unsigned char *name;
                const unsigned char *data;
                int len;
                u8_t http_header_included;
             };

            */

            LinkedListNode<FileDescriptor> node = files.First;

            while (node != null)
            {
                bool isFirst = node.Previous == null;
                FileDescriptor file = node.Value;

                result.Append($"const struct fsdata_file file__{file.FileNameInCFormat}[] = {{{{{newline}");
                if (isFirst)
                    result.Append($"\tfile_NULL,{newline}");
                else
                    result.Append($"\tfile__{node.Previous.Value.FileNameInCFormat},{newline}");

                result.Append($"\tdata__{file.FileNameInCFormat},{newline}");
                result.Append($"\tdata__{file.FileNameInCFormat} + {file.ServerFileNameLen},{newline}");
                result.Append($"\tsizeof(data__{file.FileNameInCFormat}) - {file.ServerFileNameLen},{newline}");
                result.Append($"\t1{newline}");
                result.Append($"}}}};{newline}");

                result.Append(newline);

                node = node.Next;
            }

            result.Append(newline);

            result.Append($"#define FS_ROOT file__{files.Last.Value.FileNameInCFormat}{newline}");
            result.Append($"#define FS_NUMFILES {files.Count}");

            result.Append(newline);

            return result.ToString();
        }


        private static void GetFiles(string folderName, LinkedList<FileDescriptor> files)
        {
            foreach (var dir in Directory.GetDirectories(folderName))
                GetFiles(dir, files);

            foreach (var file in Directory.GetFiles(folderName))
                files.AddLast(new FileDescriptor(file));
        }

        private static ASCIIResult ToHEXFormat(string data)
        {
            var bytes = Encoding.ASCII.GetBytes(data);

            return ToHEXFormat(bytes);
        }

        private static ASCIIResult ToHEXFormat(byte[] data)
        {
            var asciiData = String.Join(",", data.Select((n, i) =>
            {
                var res = $"0x{n:x2}";

                if (i != 0 && i % BYTES_PER_LINE == 0)
                    res += Environment.NewLine;

                return res;
            }));

            return new ASCIIResult(asciiData, data.Count());
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var creator = new FSDataCreator();
            var result = creator.Create();

            File.WriteAllText("fsdata.c", result);
        }
    }
}
