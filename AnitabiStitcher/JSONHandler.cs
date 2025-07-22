using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace AnitabiStitcher
{
    internal static class JSONHandler
    {
        public static string Serialize(int size, string fp, string op)
        {
            var data = new JSONData
            {
                fontSize = size,
                fontPath = fp,
                outputPath = op,
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(data, options);

            return jsonString;

            // 写入项目同目录下（bin\Debug\netX 目录）
            //string fileName = "data.json";
            //File.WriteAllText(fileName, jsonString);

            //Console.WriteLine($"写入完成：{fileName}");
        }

        public static void DeSerialize(string js, ref int size, ref string fp, ref string op)
        {
            string jsonString = js;
            try
            {
                var data = JsonSerializer.Deserialize<JSONData>(jsonString);

                if (data == null)
                {
                    Console.WriteLine("JSON 格式不匹配或为空。");
                    return;
                }

                size = data.fontSize;
                fp = data.fontPath;
                op = data.outputPath;

            }
            catch (JsonException ex)
            {
                Console.WriteLine($"解析出错：{ex.Message}");
            }
        }
        
    }

    public class JSONData
    {
        public int fontSize { get; set; }
        public string fontPath { get; set; }
        public string outputPath { get; set; }
    }



}

