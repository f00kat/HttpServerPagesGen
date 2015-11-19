using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServerPagesGen
{
    class Program
    {
        const int BYTES_PER_LINE = 16;

        static void Main(string[] args)
        {
            string newline = Environment.NewLine;

            var result = new StringBuilder();

            result.Append($"#include \"fs.h\" {newline}");
            result.Append($"#include \"lwip/def.h\" {newline}");
            result.Append($"#include \"fsdata.h\" {newline}");

            result.Append(newline);

            result.Append("#define file_NULL (struct fsdata_file *) NULL");

            result.Append(newline);

            var fileName = "index.html";
            var formatedFileName = fileName.Replace('.', '_');

            var fileData = File.ReadAllText(fileName);

            result.Append($"static const unsigned char data_{formatedFileName}[] = {{ {newline}");

            result.Append($"/* {fileName} */ {newline}");
            result.Append(FormatData(fileName));

            result.Append(newline);

            result.Append($"/* HTTP header */ {newline}");
            result.Append(FormatData($"HTTP/1.0 200 OK"));

            result.Append(newline);

            result.Append($"/* Server */ {newline}");
            result.Append(FormatData($"Server: lwIP/1.3.1"));

            result.Append(newline);

            result.Append($"/* Content-type */ {newline}");
            result.Append(FormatData($"Content-type: text/html"));

            result.Append(newline);
            result.Append(newline);

            result.Append(FormatData(fileData));

            result.Append("};");


            Console.WriteLine(result);

            var res = result.ToString();
        }

        private static string FormatData(string data)
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
}
