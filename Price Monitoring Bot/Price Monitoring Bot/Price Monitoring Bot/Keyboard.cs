using Telegram.Bot.Types.ReplyMarkups;

namespace Price_Monitoring_Bot
{
    static class Keyboard
    {
        // метод объединения словарей
        static Dictionary<string, string> MergeDict(Dictionary<string, string> d1, Dictionary<string, string> d2)
        {
            foreach (var (key, value) in d2)
            {
                d1.Add(key, value);
            }
            return d1;
        }

        // метод создания клавиатуры кнопок
        static public InlineKeyboardMarkup MakeKeyBoard(Dictionary<string, string> TandCBD)
        {
            List<List<InlineKeyboardButton>> kb = new List<List<InlineKeyboardButton>> { };
            foreach (var (key, value) in TandCBD)
            {
                kb.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text: key, callbackData: value) });
            }
            return new InlineKeyboardMarkup(kb);
        }

        // словари с названием кнопок и их CallBackData
        static Dictionary<string, string> 
            menu = new Dictionary<string, string>
        {
            { "Добавить товар","добавить" },
            { "Отслеживаемые товары", "отслеживаемые" },
            { "Инструкция", "инструкция" }
        },
            tracked_products = new Dictionary<string, string>
        {
            { "Изменить порог цены", "порог"},
            { "Узнать прогноз", "прогноз"},
            { "Удалить товар", "удалить" }
        },
            backTo_add = new Dictionary<string, string> { { "Назад", "добавить" } },
            backTo_tracked = new Dictionary<string, string> { { "Назад", "отслеживаемые" } },
            backTo_menu = new Dictionary<string, string> { { "Назад", "меню" } };


        public static InlineKeyboardMarkup Menu = MakeKeyBoard(menu),
            TrackedProducts = MakeKeyBoard(MergeDict(tracked_products, backTo_menu)),
            BackMenu = MakeKeyBoard(backTo_menu),
            BackAdd = MakeKeyBoard(backTo_add),
            BackTracked = MakeKeyBoard(backTo_tracked);
    }
}