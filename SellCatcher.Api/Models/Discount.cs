namespace  SellCatcher.Api.Models
{
    public class Discount
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string Product { get; set; } = ""; 
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public DateTime ValidUntil { get; set; }
    }
}
