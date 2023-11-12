namespace Price_Monitoring_Bot
{
    class User
    {
        public long ID { get; set; }
        public string State { get; set; }
        public List<Product> Products { get; set; }
        
        public int MenuMessageID  { get; set; }
        public int SelectedProductID  { get; set; }
    }
}
