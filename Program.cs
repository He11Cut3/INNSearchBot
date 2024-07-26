using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    static ITelegramBotClient botClient;
    static LastCommand lastCommand = new LastCommand();

    static async Task Main()
    {
        string botToken = ConfigurationManager.AppSettings["TelegramBotToken"];
        botClient = new TelegramBotClient(botToken);

        var me = await botClient.GetMeAsync();

        using (var cts = new CancellationTokenSource())
        {
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                new ReceiverOptions
                {
                    AllowedUpdates = Array.Empty<UpdateType>()
                },
                cancellationToken: cts.Token
            );

            Console.WriteLine("Нажмите любую кнопку для выхода.");
            Console.ReadKey();

            cts.Cancel();
        }
    }

    //Команды
    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            var messageText = update.Message.Text;
            var chatId = update.Message.Chat.Id;

            string responseMessage = "";
            switch (messageText.ToLower())
            {
                case "/start":
                    responseMessage = "Добро пожаловать! Используйте /help для получения списка доступных команд.";
                    lastCommand = new LastCommand { Command = "/start" };
                    break;
                case "/help":
                    responseMessage = "Доступные команды:\n/start - Начать\n/help - Справка\n/hello - Контактная информация\n/inn <ИНН> - Информация о компании\n/last - Повторить последнее действие\n/okved <ИНН> - Информация об оквэдах";
                    lastCommand = new LastCommand { Command = "/help" };
                    break;
                case "/hello":
                    responseMessage = "ФИ - Хайрутдинов Руслан\nПочта: ruslan.off89@mail.ru\nGitHUB - https://github.com/He11Cut3";
                    lastCommand = new LastCommand { Command = "/hello" };
                    break;
                case var text when text.StartsWith("/inn"):
                    var inns = text.Replace("/inn", "").Trim().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (inns.Length > 0)
                    {
                        responseMessage = await GetCompanyInfoByINN(inns);
                        lastCommand = new LastCommand { Command = text, Data = inns };
                    }
                    else
                    {
                        responseMessage = "Пожалуйста, укажите хотя бы один ИНН.";
                    }
                    break;
                case var text when text.StartsWith("/okved"):
                    inns = text.Replace("/okved", "").Trim().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (inns.Length > 0)
                    {
                        responseMessage = await GetOkvedInfoByINN(inns);
                        lastCommand = new LastCommand { Command = text, Data = inns };
                    }
                    else
                    {
                        responseMessage = "Пожалуйста, укажите хотя бы один ИНН.";
                    }
                    break;
                case "/last":
                    responseMessage = await RepeatLastCommand();
                    break;
                default:
                    responseMessage = "Команда не распознана. Используйте /help для получения списка доступных команд.";
                    break;
            }

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: responseMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }
    }
   
    //Обработка ошибок
    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        string errorMessage;

        switch (exception)
        {
            case ApiRequestException apiRequestException:
                errorMessage = $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}";
                break;
            default:
                errorMessage = exception.ToString();
                break;
        }

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }

    //Информация об компании по INN
    static async Task<string> GetCompanyInfoByINN(string[] inns)
    {
        var apiKey = ConfigurationManager.AppSettings["ApiINN"];
        var apiUrl = ConfigurationManager.AppSettings["ApiURL"];
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Token {apiKey}");

        var results = new List<string>();

        foreach (var inn in inns)
        {
            try
            {
                var content = new StringContent($"{{\"query\":\"{inn}\"}}", System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, content);
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response for INN {inn}: {json}");

                if (response.IsSuccessStatusCode)
                {
                    var companyData = JsonConvert.DeserializeObject<DadataResponse>(json);
                    if (companyData?.suggestions != null && companyData.suggestions.Count > 0)
                    {
                        foreach (var suggestion in companyData.suggestions)
                        {
                            results.Add($"Компания: {suggestion.value}\nАдрес: {suggestion.data.address.value}");
                        }
                    }
                    else
                    {
                        results.Add($"ИНН: {inn} - информация не найдена.");
                    }
                }
                else
                {
                    results.Add($"ИНН: {inn} - ошибка при получении данных. Код ошибки: {response.StatusCode}");
                }
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"Ошибка чтения JSON для ИНН {inn}: {ex.Message}\nJSON");
                results.Add($"ИНН: {inn} - ошибка обработки данных.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка для ИНН {inn}: {ex.Message}");
                results.Add($"ИНН: {inn} - непредвиденная ошибка.");
            }
        }

        return string.Join("\n\n", results);
    }

    //Информация об оквэдах по INN
    static async Task<string> GetOkvedInfoByINN(string[] inns)
    {
        var apiKey = ConfigurationManager.AppSettings["ApiINN"];
        var apiUrl = ConfigurationManager.AppSettings["ApiURL"];
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Token {apiKey}");

        var results = new List<string>();

        foreach (var inn in inns)
        {
            try
            {
                var content = new StringContent($"{{\"query\":\"{inn}\"}}", System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, content);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var companyData = JsonConvert.DeserializeObject<DadataResponse>(json);
                    if (companyData?.suggestions != null && companyData.suggestions.Count > 0)
                    {
                        foreach (var suggestion in companyData.suggestions)
                        {
                            var address = suggestion.data?.address?.value ?? "Адрес не найден";
                            var okved = suggestion.data?.okved ?? "Информация по ОКВЭД отсутствует";

                            results.Add($"Компания: {suggestion.value}\nОКВЭД: {okved}");
                        }
                    }
                    else
                    {
                        results.Add($"ИНН: {inn} - информация не найдена.");
                    }
                }
                else
                {
                    results.Add($"ИНН: {inn} - ошибка при получении данных. Код ошибки: {response.StatusCode}");
                }
            }
            catch (JsonReaderException ex)
            {
                results.Add($"ИНН: {inn} - ошибка обработки данных.");
            }
            catch (Exception ex)
            {
                results.Add($"ИНН: {inn} - непредвиденная ошибка.");
            }
        }

        return string.Join("\n\n", results);
    }

    //Выполнение последней команды
    static async Task<string> RepeatLastCommand()
    {
        if (lastCommand == null || string.IsNullOrEmpty(lastCommand.Command))
        {
            return "Нет последней команды для повторения.";
        }

        switch (lastCommand.Command.ToLower())
        {
            case var cmd when cmd.StartsWith("/inn") && lastCommand.Data != null:
                return await GetCompanyInfoByINN(lastCommand.Data);
            case "/start":
                return "Добро пожаловать! Используйте /help для получения списка доступных команд.";
            case "/help":
                return "Доступные команды:\n/start - Начать\n/help - Справка\n/hello - Контактная информация\n/inn <ИНН> - Информация о компании\n/last - Повторить последнее действие\n/okved <ИНН> - Информация об оквэдах";
            case "/hello":
                return "ФИ - Хайрутдинов Руслан\nПочта: ruslan.off89@mail.ru\nGitHUB - https://github.com/He11Cut3";
            default:
                return $"Повторение команды: {lastCommand.Command}";
        }
    }

    //Модель классов
    public class LastCommand
    {
        public string Command { get; set; }
        public string[] Data { get; set; } // Может использоваться для хранения ИНН или других данных
    }

    public class DadataResponse
    {
        public List<Suggestion> suggestions { get; set; }
    }

    public class Suggestion
    {
        public string value { get; set; }
        public SuggestionData data { get; set; }
    }

    public class SuggestionData
    {
        public AddressData address { get; set; }
        public string okved { get; set; } // ОКВЭД как строка
    }

    public class AddressData
    {
        public string value { get; set; }
    }

    public class Okved
    {
        public string code { get; set; }
        public string name { get; set; }
    }
}
