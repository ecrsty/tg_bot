using Newtonsoft.Json;
using Price_Monitoring_Bot.Parcer;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Price_Monitoring_Bot
{
    internal class Program
    {
        // инициализация бота, списка пользователей и парсеров
        private static string token { get; set; } = "TOKEN";
        private static TelegramBotClient Bot;

        private static List<User> users;

        private static LamaParser lamaParser;
        private static LeoParser leoParser;
        private static UlybkaParser ulybkaParser;
        private static MgnlParser mgnlParser;
        private static OgoParser ogoParser;
        private static BmsParser bmsParser;

        // файл для сохранения данных
        private static string filePath = @"PATH";

        const string textStart = "Добро пожаловать! Данный бот разработан с целью упростить процесс отслеживания " +
                            "изменения цен на товары. Для ознакомления с функционалом воспользуйтесь кнопкой \"Инструкция\".";

        const string instruction = "- <b>Инструкция</b> -\nЧтобы добавить товар для отслеживания, воспользуйтесь кнопкой <b><i>\"Добавить товар\"</i></b> и выберите магазин из предложенного каталога. После этого отправьте ссылку на товар из выбранного магазина.\n\n" +
                        "Добавив товары, Вы сможете посмотреть информацио о них, нажав кнопку <b><i>\"Отслеживаемые товары\"</i></b>.\nВ этом разделе будут доступны следующие кнопки:\n" +
                        "- <b><i>Изменить порог цены</i></b>\nПозволяет установить уровень цены, с которым будет сравниваться цена во время мониторинга. Если Новая цена окажется ниже установленного значения, придет уведомление.\n" +
                        "- <b><i>Узнать прогноз</i></b>\nНа основе полученных за время мониторинга данных выведется приблизительная стоимость товара в будущем\n" +
                        "- <b><i>Удалить товар</i></b>\nУдаляет выбранный товар из списка отслеживаемых\n\n";

        const string lamaLink = "https://www.lamoda.ru/",
            leoLink = "https://leonardo.ru/",
            ulybkaLink = "https://www.r-ulybka.ru/",
            mgnlLink = "https://shop.mgnl.ru/",
            ogoLink = "https://ogo1.ru/",
            bmsLink = "https://www.bestmebelshop.ru/";

        const string add_text = $"<b>Выберите магазин:</b>\n- Магнолия - {mgnlLink}\n" +
                                      $"- Lamoda - {lamaLink}\n" +
                                      $"- Леонардо - {leoLink}\n" +
                                      $"- Улыбка радуги - {ulybkaLink}\n" +
                                      $"- Ого! - {ogoLink}\n" +
                                      $"- BestMebelShop - {bmsLink}\n" +
                                      "<i>Для добавления товара отправьте ссылку на товар из этого магазина.</i>";

        static void Main(string[] args)
        {
            lamaParser = new LamaParser();
            leoParser = new LeoParser();
            ulybkaParser = new UlybkaParser();
            mgnlParser = new MgnlParser();
            ogoParser = new OgoParser();
            bmsParser = new BmsParser();

            //using CancellationTokenSource cts = new();
            var cancelTok = new CancellationTokenSource();
            var cts = cancelTok.Token;

            if (System.IO.File.Exists(filePath))
            {
                // файл существует, выполняем десериализацию
                string json = System.IO.File.ReadAllText(filePath);
                users = JsonConvert.DeserializeObject<List<User>>(json);
            }
            else
            {
                // файл не существует, создаем новый список пользователей
                users = new List<User>();
            }

            Bot = new TelegramBotClient(token);
            // вывод в консоль, что бот запущен id: {me.Id},
            var me = Bot.GetMeAsync().Result;
            Console.WriteLine($"Hi! I'm {me.FirstName}, UsName: {me.Username}");

            // запуск обработчика обновлений и парсера
            Bot.StartReceiving(Update, Error, cancellationToken: cts);
            Task.Run(async () => await StartMonitoring(Bot));

            Console.ReadLine();
            cancelTok.Cancel();   
        }

        // обработка сообщения от пользователя
        async public static Task Update(ITelegramBotClient client, Update update, CancellationToken cts)
        {
            try
            {
                // обработка текста
                if (update.Type == UpdateType.Message)
                {
                    TextMessageHandler(update, client);
                }
                // обработка коллбэка с кнопок
                if (update.Type == UpdateType.CallbackQuery)
                {
                    CallbackHandler(update, client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении обновления {update}: {ex.Message}");
            }
        }

        // обработчик исключений
        private static Task Error(ITelegramBotClient client, Exception exception, CancellationToken cts)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        // метод сохранения данных
        public static void SaveData()
        {
            string serializedJson = JsonConvert.SerializeObject(users);
            System.IO.File.WriteAllText(filePath, serializedJson);
        }

        // обработчик текстовых сообщений
        async public static void TextMessageHandler(Update update, ITelegramBotClient client)
        {
            var message = update.Message;
            var chatID = message.Chat.Id;

            var user = GetOrCreateUser(chatID);

            if (message.Text != null)
            {
                var msgText = message.Text.ToLower();
                Console.WriteLine($"{message.Chat.Username} | {chatID} | {msgText} | {DateTime.Now.ToString("g")}");

                switch (msgText)
                {
                    case ("/start"):
                    case ("/menu"):
                    case ("меню"):
                        user.State = null;
                        await client.SendTextMessageAsync(chatID, textStart, replyMarkup: Keyboard.Menu);
                        break;
                    case ("добавить"):
                    case ("добавить товар"):
                        user.State = "addProduct";
                        await client.SendTextMessageAsync(chatID, add_text, parseMode: ParseMode.Html, replyMarkup: Keyboard.BackMenu);
                        break;
                    default:
                        if (user.State != null)
                        {
                            bool isOk;
                            switch (user.State)
                            {
                                case ("addProduct"):
                                    string shop = DefineShop(msgText);
                                    if (shop == null)
                                        isOk = false;
                                    else isOk = AddProduct_Method(msgText, user, shop); 

                                    if (isOk)
                                    {
                                        await client.SendTextMessageAsync(chatID,
                                            $"Товар {msgText} добавлен. \nМожете добавить еще товар!",
                                            replyMarkup: Keyboard.BackAdd);
                                        break;
                                    }
                                    await client.SendTextMessageAsync(chatID,
                                        $"Не удалось обработать ссылку.\nВозможно, ссылка некорректна или уже добавлена: {msgText}",
                                        replyMarkup: Keyboard.BackMenu);
                                    break;

                                case ("deleteProduct"):
                                    isOk = DelProduct_Method(msgText, user, out string name); 
                                    if (isOk)
                                    {
                                        if (user.Products.Count == 0)
                                        {
                                            user.State = null;
                                            await client.SendTextMessageAsync(chatID, $"Товар {name} удален. \nНет отслеживаемых товаров",
                                                replyMarkup: Keyboard.BackMenu);
                                        }
                                        else
                                        {
                                            string yourProducts = GetUserProducts(user);
                                            await client.SendTextMessageAsync(chatID,
                                                $"Товар {name} удален.\n\n<b>Ваши товары</b>:\n\n{yourProducts}\n<b>Отправьте номер товара для удаления</b>",
                                                parseMode: ParseMode.Html, replyMarkup: Keyboard.BackTracked);
                                        }
                                        SaveData();
                                        break;
                                    }
                                    else goto case ("numberError");

                                case ("changeLevel"):
                                    if (CheckUserNumber(msgText, user))
                                    {
                                        user.SelectedProductID = Convert.ToInt32(msgText);
                                        name = user.Products[user.SelectedProductID - 1].Name;
                                        await client.SendTextMessageAsync(chatID,
                                            $"Выбран {name} для изменения порога. Введите новое значение порога цены.",
                                            replyMarkup: Keyboard.BackTracked);
                                        user.State = "changeLevelNumber";
                                        SaveData();
                                        break;
                                    }
                                    else goto case ("numberError");

                                case ("changeLevelNumber"):
                                    isOk = ChangeLevel_Method(msgText, 
                                        user.SelectedProductID, user, out name); 
                                    if (isOk)
                                    {
                                        await client.SendTextMessageAsync(chatID,
                                            $"У товара {name} изменен уровень на {msgText}",
                                            replyMarkup: Keyboard.BackTracked);
                                        SaveData();
                                        break;
                                    }
                                    else goto case ("numberError");

                                case ("predict"):
                                    isOk = PredictProduct_Method(msgText, user, 
                                        out name, out double pred); 
                                    if (isOk)
                                    {
                                        await client.SendTextMessageAsync(chatID, 
                                            $"Предпологаемая цена {name} = {pred}",
                                            replyMarkup: Keyboard.BackTracked);
                                        break;
                                    }
                                    else goto case ("numberError");

                                case ("numberError"):
                                    await client.SendTextMessageAsync(chatID,
                                        $"Некорректный ввод числа: {msgText}",
                                        replyMarkup: Keyboard.BackTracked);
                                    break;
                            }
                        }
                        else
                            await client.SendTextMessageAsync(chatID,
                                "Пожалуйста, воспользуйтесь кнопками или вводите данные внимательнее :(",
                                replyMarkup: Keyboard.BackMenu);
                        break;
                }
            }
        }
        
        // обработчик данных обратного вызова
        async public static void CallbackHandler(Update update, ITelegramBotClient client)
        {
            var msg = update.CallbackQuery.Data;
            var chatID = update.CallbackQuery.Message.Chat.Id;
            var mesID = update.CallbackQuery.Message.MessageId;

            var user = GetOrCreateUser(chatID);

            Console.WriteLine(msg);
            string yourProducts;
            switch (msg)
            {
                case ("меню"):
                    user.State = null;
                    await client.EditMessageTextAsync(chatID, mesID, textStart, replyMarkup: Keyboard.Menu);
                    break;

                case ("инструкция"):
                    await client.EditMessageTextAsync(chatID, mesID, instruction, ParseMode.Html, replyMarkup: Keyboard.BackMenu);
                    break;

                case ("добавить"):
                    user.State = "addProduct";
                    await client.EditMessageTextAsync(chatID, mesID, add_text, ParseMode.Html, replyMarkup: Keyboard.BackMenu);
                    break;

                case ("отслеживаемые"):
                    user.State = null;
                    if (user.Products.Count == 0)
                    {
                        await client.EditMessageTextAsync(chatID, mesID, "Нет отслеживаемых товаров",
                            replyMarkup: Keyboard.BackMenu);
                    }
                    else
                    {
                        yourProducts = GetUserProducts(user);
                        await client.EditMessageTextAsync(chatID, mesID, $"<b>Ваши товары</b>:\n\n{yourProducts}",
                            ParseMode.Html, replyMarkup: Keyboard.TrackedProducts);
                    }
                    break;

                case ("удалить"):
                    user.State = "deleteProduct";
                    yourProducts = GetUserProducts(user);
                    await client.EditMessageTextAsync(chatID, mesID, $"<b>Ваши товары</b>:\n\n{yourProducts}\n<b>Отправьте номер товара для удаления</b>",
                        ParseMode.Html, replyMarkup: Keyboard.BackTracked);
                    user.MenuMessageID = mesID;
                    break;

                case ("порог"):
                    user.State = "changeLevel";
                    yourProducts = GetUserProducts(user);
                    await client.EditMessageTextAsync(chatID, mesID, $"<b>Ваши товары</b>:\n\n{yourProducts}\n<b>Отправьте номер товара для изменения порога цены</b>",
                        ParseMode.Html, replyMarkup: Keyboard.BackTracked);
                    user.MenuMessageID = mesID;
                    break;

                case ("прогноз"):
                    user.State = "predict";
                    yourProducts = GetUserProducts(user);
                    await client.EditMessageTextAsync(chatID, mesID, $"<b>Ваши товары</b>:\n\n{yourProducts}\n<b>Отправьте номер товара для получения прогноза</b>",
                        ParseMode.Html, replyMarkup: Keyboard.BackTracked);
                    user.MenuMessageID = mesID;
                    break;
            }
        }

        // проверить, новый ли пользователь
        public static User GetUser(ChatId chatID)
        {
            foreach (User user in users)
            {
                if (user.ID == Convert.ToInt64(chatID.ToString()))
                    return user;
            }
            return null;
        }

        // получить экземпляр класса User
        public static User GetOrCreateUser(long chatID)
        {
            var user = GetUser(chatID);
            if (user == null)
            {
                user = new User();
                user.ID = Convert.ToInt64(chatID.ToString());
                user.Products = new List<Product>();
                users.Add(user);
                SaveData();
            }
            return user;
        }

        // метод определения магазина
        public static string DefineShop(string msgText)
        {
            string shop;
            if (msgText.Contains(lamaLink))
                shop = "lamoda";
            else if (msgText.Contains(ulybkaLink))
                shop = "ulybka";
            else if (msgText.Contains(leoLink))
                shop = "leonardo";
            else if (msgText.Contains(mgnlLink))
                shop = "mgnl";
            else if (msgText.Contains(ogoLink))
                shop = "ogo";
            else if (msgText.Contains(bmsLink))
                shop = "bms";
            else shop = null;
            return shop;
        }
        
        // метод добавления товара
        static bool AddProduct_Method(string msgText, User user, string shop)
        {
            foreach (Product pr in user.Products)
            {
                if (pr.Url == msgText)
                    return false;
            }

            List<string> link = new List<string>() { msgText };
            (string name, double cur_pr, string date, string url) data = (null, 0, null, null);
            List<(string, double, string, string)> parceList = new List<(string, double, string, string)>();

            switch (shop)
            {
                case ("lamoda"):
                    parceList = lamaParser.Parse(link);
                    break;
                case ("leonardo"):
                    parceList = leoParser.Parse(link);
                    break;
                case ("ulybka"):
                    parceList = ulybkaParser.Parse(link);
                    break;
                case ("mgnl"):
                    parceList = mgnlParser.Parse(link);
                    break;
                case ("ogo"):
                    parceList = ogoParser.Parse(link);
                    break;
                case ("bms"):
                    parceList = bmsParser.Parse(link);
                    break;
            }

            if (parceList.Count > 0)
            {
                data = parceList[0];
            }
            if (data.name != null)
            {
                user.Products.Add(new Product
                {
                    Url = msgText,
                    Shop = shop,
                    Name = data.name,
                    CurrentPrice = data.cur_pr,
                    Date = data.date,
                    Prices = new List<double> { data.cur_pr },
                    LevelPrice = data.cur_pr
                });
                SaveData();
                return true;
            }
            return false;
        }

        // метод проверки числа пользователя
        static bool CheckUserNumber(string msgText, User user)
        {
            int cnt = user.Products.Count;
            try
            {
                int n = Convert.ToInt32(msgText);
                if (n > cnt || n <= 0)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке номера {msgText}: {ex.Message}");
                return false;
            }
        }

        // метод изменения порога цены
        static bool ChangeLevel_Method(string newLevel, int poductId, User user, out string name)
        {
            name = null;
            try
            {
                int n = poductId;
                user.Products[n - 1].LevelPrice = Convert.ToDouble(newLevel);
                name = user.Products[n - 1].Name;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке порога цены {newLevel}: {ex.Message}");
                return false;
            }
        }

        // метод получения строки с товарами пользователя
        public static string GetUserProducts(User user)
        {
            string yourProducts = "";
            int i = 1;
            foreach (var pr in user.Products)
            {
                yourProducts += $"[{i++}] {pr.Name}\n";
                yourProducts += $"{pr.Url}\n";
                yourProducts += $"{pr.CurrentPrice} ₽ : ";
                yourProducts += $"{pr.Date}\n";
                yourProducts += $"Порог цены: {pr.LevelPrice}\n";
                yourProducts += "\n";
            }
            return yourProducts;
        }
        
        // метод удаления товара
        static bool DelProduct_Method(string msgText, User user, out string name)
        {
            name = null;
            if (CheckUserNumber(msgText, user))
            {
                int n = Convert.ToInt32(msgText);
                name = user.Products[n - 1].Name;
                user.Products.RemoveAt(n - 1);
                SaveData();
                return true;
            }
            return false;
        }

        // метод получения прогноза
        static bool PredictProduct_Method(string msgText, User user, out string name, out double pred)
        {
            name = null;
            pred = 0;
            if (CheckUserNumber(msgText, user))
            {
                int n = Convert.ToInt32(msgText);
                name = user.Products[n - 1].Name;
                pred = Extrapolation(user.Products[n - 1].Prices);
                return true;
            }
            return false;
        }
        
        // экстраполяция
        static double Extrapolation(List<double> data)
        {
            // n * a     + sum_x  * b = sum_y 
            // sum_x * a + sum_xx * b = sum_xy

            double sum_y = 0, sum_x = 0, sum_xx = 0, sum_xy = 0, a = 0, b = 0;
            int n = data.Count;
            int x = 0;
            if (data.Count >= 1000)
                n = 1000;
            for (int i = data.Count - n; i < data.Count; i++)
            {
                x++;
                sum_y += data[i];
                sum_x += x;
                sum_xx += x * x;
                sum_xy += x * data[i];
            }

            double det = n * sum_xx - sum_x * sum_x; // определитель матрицы системы
            if (det == 0 || data.Count <= 1)
            {
                return data[0];
                //Console.WriteLine("Система не имеет решений");
            }
            else
            {
                a = (sum_y * sum_xx - sum_xy * sum_x) / det;
                b = (n * sum_xy - sum_y * sum_x) / det;
            }
            double forecast = a + b * (x+1);
            return Math.Round(forecast, 2);
        }

        // метод мониторинга цен
        async public static Task StartMonitoring(TelegramBotClient client)
        {
            while (true)
            {
                List<string> lamaUrls = new List<string>(),
                    leoUrls = new List<string>(),
                    ulybkaUrls = new List<string>(),
                    mgnlUrls = new List<string>(),
                    ogoUrls = new List<string>(),
                    bmsUrls = new List<string>();

                foreach (User u in users)
                {
                    foreach (Product pr in u.Products)
                    {
                        switch (pr.Shop)
                        {
                            case ("lamoda"):
                                lamaUrls.Add(pr.Url);
                                break;
                            case ("leonardo"):
                                leoUrls.Add(pr.Url);
                                break;
                            case ("ulybka"):
                                ulybkaUrls.Add(pr.Url);
                                break;
                            case ("mgnl"):
                                mgnlUrls.Add(pr.Url);
                                break;
                            case ("ogo"):
                                ogoUrls.Add(pr.Url);
                                break;
                            case ("bms"):
                                bmsUrls.Add(pr.Url);
                                break;
                        }
                    }
                }

                List<(string name, double cur_pr, string date, string url)> data =
                    new List<(string name, double cur_pr, string date, string url)>();
                data.AddRange(lamaParser.Parse(lamaUrls));
                data.AddRange(leoParser.Parse(leoUrls));
                data.AddRange(ulybkaParser.Parse(ulybkaUrls));
                data.AddRange(mgnlParser.Parse(mgnlUrls));
                data.AddRange(ogoParser.Parse(ogoUrls));
                data.AddRange(bmsParser.Parse(bmsUrls));

                foreach (User u in users)
                {
                    foreach (Product pr in u.Products)
                    {
                        foreach (var req in data)
                        {
                            if (pr.Url == req.url)
                            {
                                pr.Name = req.name;
                                pr.Prices.Add(req.cur_pr);
                                pr.CurrentPrice = req.cur_pr;
                                pr.Date = req.date;

                                if (pr.LevelPrice > pr.CurrentPrice)
                                {
                                    await client.SendTextMessageAsync(u.ID,
                                        $"Цена на товар снижена!\n{pr.Name}\n{pr.Url}\nНовая цена: {pr.CurrentPrice} ₽\nПорог: {pr.LevelPrice}\nДата проверки: {pr.Date}",
                                        replyMarkup: Keyboard.BackTracked);
                                }
                            }
                        }
                    }
                }
                SaveData();
                await Task.Delay(1000 * 60 * 60 * 5);
            }
        }
    }
}