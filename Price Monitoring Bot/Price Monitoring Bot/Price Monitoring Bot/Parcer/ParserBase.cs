using HtmlAgilityPack;
using System.Globalization;

namespace Parser
{
    public interface IParser { List<(string name, double cur_pr, string date, string url)> Parse(List<string> urls); }

    public abstract class ParserBase : IParser
    {
        protected HtmlWeb web;
        protected ParserBase()
        {
            web = new HtmlWeb();
        }

        // абстрактный метод парсинга, ббудет изменяться в зависимости от сайта
        public abstract List<(string name, double cur_pr, string date, string url)> Parse(List<string> urls);

        // фильтр для очистки цен от лишних символов
        protected string Filter(string s, string c = "., 0123456789")
        {
            int symbolIndex = s.IndexOf("&#x20bd;");
            if (symbolIndex >= 0)
            {
                s = s.Substring(0, symbolIndex);
            }
            foreach (char ch in s)
            {
                if (!c.Contains(ch))
                {
                    s = s.Replace(ch, ' ');
                }
            }
            return s;
        }

        // обработка полученных значений
        protected (string name, double cur_pr, string date) ProcessResult(string name_text, string price_text)
        {
            (string name, double cur_pr, string date) result;

            if (name_text != null && price_text != null)
            {
                name_text = name_text.Replace("&quot;", "\"");
                price_text = Filter(price_text).Replace(",", ".").Replace(" ", "");
                double price_value;
                double.TryParse(price_text, NumberStyles.Float, CultureInfo.InvariantCulture, out price_value);
                string date = DateTime.Now.ToString("g");
                result = (name_text, price_value, date);
                return result;
            }
            else
            {
                return (null, 0, null);
            }
        }
    }
}
