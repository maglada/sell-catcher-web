using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;

class Program
{
    static void Main(string[] args)
    {
        string apiShop1Url = "";
        string apiShop2Url = "";

        string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shopdata.json");

        var apiJson = new ApiJson(apiShop1Url, apiShop2Url, jsonPath);
        var db = new DatabaseService();
        var priceService = new PriceService(db);

        try
        {
            var (shop1, shop2) = apiJson.LoadData();

            priceService.LoadToDatabase(shop1, shop2);

            priceService.CompareAllAndSave();

        }
        catch (Exception)
        {

        }
    }
}