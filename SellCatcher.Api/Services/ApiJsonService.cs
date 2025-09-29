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
        private readonly string _apiShop1Url;
        private readonly string _apiShop2Url;
        private readonly string _jsonPath;

        public ApiJson(string apiShop1Url, string apiShop2Url, string jsonPath)
        {
            _apiShop1Url = apiShop1Url.Trim();
            _apiShop2Url = apiShop2Url.Trim();
            _jsonPath = string.IsNullOrWhiteSpace(jsonPath) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shopdata.json") : jsonPath;
        }

        public (List<Shop1Product> shop1, List<Shop2Product> shop2) LoadData()
        {
            
            if (!string.IsNullOrWhiteSpace(_apiShop1Url) && !string.IsNullOrWhiteSpace(_apiShop2Url))
            {
                try
                {
                    using var client = new HttpClient();
                    var s1 = client.GetStringAsync(_apiShop1Url).Result;
                    var s2 = client.GetStringAsync(_apiShop2Url).Result;

                    var list1 = JsonSerializer.Deserialize<List<Shop1Product>>(s1);
                    var list2 = JsonSerializer.Deserialize<List<Shop2Product>>(s2);

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
                    if (wrapper?.Shop1 != null && wrapper?.Shop2 != null)
                        return (wrapper.Shop1, wrapper.Shop2);
                }
                catch (Exception)
                {
                    
                }
            }
            throw new Exception("Даних нема");
        }

        private class JsonWrapper
        {
            public List<Shop1Product>? Shop1 { get; set; }
            public List<Shop2Product>? Shop2 { get; set; }
        }
    }
}