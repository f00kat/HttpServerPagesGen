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

            public string FormatedFileName
            {
                get
                {
                    return _file.Replace(DEFAULT_DIR + "\\", String.Empty)
                                .Replace(".", "_")
                                .Replace("\\", "_");
                }
            }
        }

        private const string DEFAULT_DIR = "fs";

        private const int BYTES_PER_LINE = 16;

        private string newline = Environment.NewLine;

        public FSDataCreator()
        {

        }

        public string Create()
        {
            var result = new StringBuilder();

            result.Append(GenerateHeader());

            var files = new List<FileDescriptor>();
            GetFiles(DEFAULT_DIR, files);

            result.Append(GenerateData(files));

            var res = result.ToString();

            return res;
        }

        private string GenerateHeader()
        {
            var result = new StringBuilder();

            result.Append($"#include \"fs.h\" {newline}");
            result.Append($"#include \"lwip/def.h\" {newline}");
            result.Append($"#include \"fsdata.h\" {newline}");

            result.Append(newline);

            result.Append("#define file_NULL (struct fsdata_file *) NULL");

            result.Append(newline);

            Console.WriteLine("Header generated");

            return result.ToString();
        }

        private string GenerateData(IEnumerable<FileDescriptor> files)
        {
            var result = new StringBuilder();
            foreach (var file in files)
            {
                var fileData = File.ReadAllText(file.FileName);

                result.Append($"static const unsigned char data_{file.FormatedFileName}[] = {{ {newline}");

                /* File name */
                result.Append($"/* {file.FileName} */ {newline}");
                result.Append(ToHEXFormat(file.FileName));

                result.Append(newline);

                /* HTTP Header */
                result.Append($"/* HTTP header */ {newline}");
                result.Append(ToHEXFormat($"HTTP/1.0 200 OK"));

                result.Append(newline);

                /* Server */
                result.Append($"/* Server */ {newline}");
                result.Append(ToHEXFormat($"Server: lwIP/1.3.1"));

                result.Append(newline);

                /* Content-type */
                result.Append($"/* Content-type */ {newline}");
                result.Append(ToHEXFormat($"Content-type: text/html"));

                result.Append(newline);
                result.Append(newline);

                /* Data */
                result.Append(ToHEXFormat(fileData));

                result.Append("};");

                result.Append(newline);
                result.Append(newline);

                Console.WriteLine($"file data '{file.FormatedFileName}' generated");
            }

            return result.ToString();
        }


        private static void GetFiles(string folderName, List<FileDescriptor> files)
        {
            foreach (var file in Directory.GetFiles(folderName))
                files.Add(new FileDescriptor(file));

            foreach (var dir in Directory.GetDirectories(folderName))
                GetFiles(dir, files);
        }

        private static string ToHEXFormat(string data)
        {
            return String.Join(",", Encoding.ASCII.GetBytes(data).Select((n, i) =>
            {
                var res = $"0x{n:x2}";

                if (i != 0 && i % BYTES_PER_LINE == 0)
                    res += Environment.NewLine;

                return res;
            }));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var creator = new FSDataCreator();
            creator.Create();
        }
    }
}
