using Parser;

namespace Price_Monitoring_Bot.Parcer
{
    public class MgnlParser : ParserBase
    {
        public override List<(string name, double cur_pr, string date, string url)> Parse(List<string> urls)
        {
            List<(string name, double cur_pr, string date, string url)> result = new List<(string name, double cur_pr, string date, string url)>();

            foreach (string url in urls)
            {
                try
                {
                    var htmlDoc = web.Load(url);
                    var name = htmlDoc.DocumentNode.SelectSingleNode("//h1");
                    var name_text = name.InnerText.Trim();
                    var price = htmlDoc.DocumentNode.SelectNodes("//div[@class='price_matrix_wrapper ']/div[@class='price font-bold font_mxs']");
                    var price_text = price.Last().Attributes["data-value"].Value.Trim();
                    var res = ProcessResult(name_text, price_text);
                    var r = (res.name, res.cur_pr, res.date, url);
                    result.Add(r);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке ссылки {url}: {ex.Message}");
                }
            }
            return result;
        }
    }
}
