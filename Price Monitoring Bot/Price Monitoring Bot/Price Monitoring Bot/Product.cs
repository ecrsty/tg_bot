namespace Price_Monitoring_Bot
{
    internal class Product
    {
        public string Shop { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public double CurrentPrice { get; set; }
        public string Date { get; set; }
        public List<double> Prices { get; set; }
        public double LevelPrice { get; set; }
    }
}
