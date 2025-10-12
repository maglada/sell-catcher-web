using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using SellCatcher.Api.Models;

namespace SellCatcher.Api.Services
{
    public class ApiJson
    {
        private readonly string _apiAtbUrl;
        private readonly string _apiNovusUrl;
        private readonly string _jsonPath;

        public ApiJson(string apiAtbUrl, string apiNovusUrl, string jsonPath)
        {
            _apiAtbUrl = apiAtbUrl.Trim();
            _apiNovusUrl = apiNovusUrl.Trim();
            _jsonPath = string.IsNullOrWhiteSpace(jsonPath) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shopdata.json") : jsonPath;
        }

        public (List<ATBProduct> atb, List<NOVUSProduct> novus) LoadData()
        {
            
            if (!string.IsNullOrWhiteSpace(_apiAtbUrl) && !string.IsNullOrWhiteSpace(_apiNovusUrl))
            {
                try
                {
                    using var client = new HttpClient();
                    var s1 = client.GetStringAsync(_apiAtbUrl).Result;
                    var s2 = client.GetStringAsync(_apiNovusUrl).Result;

                    var list1 = JsonSerializer.Deserialize<List<ATBProduct>>(s1);
                    var list2 = JsonSerializer.Deserialize<List<NOVUSProduct>>(s2);

                    if (list1 != null && list2 != null)
                        return (list1, list2);
                }
                catch (Exception)
                {
                    
                }
            }

            if (File.Exists(_jsonPath))
            {
                try
                {
                    var json = File.ReadAllText(_jsonPath);
                    var wrapper = JsonSerializer.Deserialize<JsonWrapper>(json);
                    if (wrapper?.atb != null && wrapper?.novus != null)
                        return (wrapper.atb, wrapper.novus);
                }
                catch (Exception)
                {
                    
                }
            }
            throw new Exception("Даних нема");
        }

        private class JsonWrapper
        {
            public List<ATBProduct>? atb { get; set; }
            public List<NOVUSProduct>? novus { get; set; }
        }
    }
}