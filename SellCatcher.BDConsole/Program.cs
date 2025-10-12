using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;

class Program
{
    //static void Main(string[] args)
    //{
    //    string apiAtbUrl = "";
    //    string apiNovusUrl = "";

    //    string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shopdata.json");

    //    var apiJson = new ApiJson(apiAtbUrl, apiNovusUrl, jsonPath);
    //    var db = new DatabaseService();
    //    var priceService = new PriceService(db);

    //    try
    //    {
    //        var (atb, novus) = apiJson.LoadData();

    //        priceService.LoadToDatabase(atb, novus);

    //        priceService.CompareAllAndSave();

    //    }
    //    catch (Exception)
    //    {

    //    }
    //}
    
    static async Task Main(string[] args)
    {
        var novuservice = new NovusService();
        await novuservice.NovusScraperToDB();
    }
}
