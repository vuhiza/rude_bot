﻿using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using PowerBot.Lite.Attributes;
using PowerBot.Lite.Handlers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using RudeBot.Managers;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Extensions;
using Autofac.Features.AttributeFilters;
using OpenAI_API;

namespace RudeBot.Handlers
{
    public class BotHandler : BaseHandler
    {
        private IUserManager _userManager { get; set; }
        private ITickerService _tickerService { get; set; }
        private ICatService _catService { get; set; }
        private TxtWordsDataset AdvicesService { get; set; }
        private static Object _topLocked { get; set; } = new Object();
        private IChatSettingsService _chatSettingsService { get; set; }

        public BotHandler(
            IUserManager userManager,
            IChatSettingsService chatSettingsService,
            ITickerService tickerService,
            ICatService catService,
            [KeyFilter(Consts.AdvicesService)] TxtWordsDataset advicesService
            )
        {
            _userManager = userManager;
            _chatSettingsService = chatSettingsService;
            _tickerService = tickerService;
            _catService = catService;
            AdvicesService = advicesService;
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("(^/start|^/help)")]
        public async Task Start()
        {
            var messageText = string.Format(Resources.InfoText, Consts.BotVersion);

            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithUrl(Resources.Page, Resources.ProjectUrl)
            });

            Message msg = await BotClient.SendTextMessageAsync(ChatId, messageText, ParseMode.Markdown, replyMarkup: keyboard);

            await Task.Delay(60 * 1000);

            await BotClient.TryDeleteMessage(msg);
            await BotClient.TryDeleteMessage(Message);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("[\\w\\-]+\\.ru")]
        public async Task DotRu()
        {
            string messageText = Resources.ruPropaganda;
            Message msg = await BotClient.SendTextMessageAsync(ChatId, messageText, replyToMessageId: Message.MessageId);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("(^карма$|^karma$)")]
        public async Task Karma()
        {
            UserChatStats userStats = await _userManager.GetUserChatStats(User.Id, ChatId);

            string replyText = userStats.BuildInfoString();

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
            await BotClient.TryDeleteMessage(Message);
        }

        [MessageReaction(ChatAction.UploadVideo)]
        [MessageHandler("шарий|шарій")]
        public async Task CockMan()
        {
            Message msg = await BotClient.SendVideoAsync(chatId: ChatId, video: Resources.CockmanVideoUrl);

            await Task.Delay(30 * 1000);
            await BotClient.TryDeleteMessage(msg);
        }

        [MessageReaction(ChatAction.UploadPhoto)]
        [MessageHandler("samsung|самсунг|сасунг")]
        public async Task Samsung()
        {
            Message msg = await BotClient.SendPhotoAsync(chatId: ChatId, photo: Resources.SamsungUrl, replyToMessageId: Message.MessageId);

            await Task.Delay(30 * 1000);
            await BotClient.TryDeleteMessage(msg);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("ё|ъ|ы|э")]
        public async Task Palanytsa()
        {
            var chatSettings = await _chatSettingsService.GetChatSettings(ChatId);

            // Ignore message forwards
            if (Message.ForwardFrom != null || Message.ForwardFromChat != null || !chatSettings.HaterussianLang)
                return;

            string replyText = Resources.Palanytsia;

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

            await Task.Delay(30 * 1000);
            await BotClient.TryDeleteMessage(msg);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("tesl|тесл")]
        public async Task Tesla()
        {

            double tickerPrice = await _tickerService.GetTickerPrice("TSLA");

            string replyText = String.Format(Resources.TeslaAgain, tickerPrice);

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler(Consts.TnxWordsRegex)]
        public async Task IncreaseKarma()
        {
            // Ignore message forwards
            if (Message.ForwardFrom != null || Message.ForwardFromChat != null)
                return;

            // Filter only reply to other user, ignore bots
            if (Message.ReplyToMessage == null || Message.ReplyToMessage.From!.Id == User.Id || Message.ReplyToMessage.From.IsBot)
                return;

            UserChatStats userStats = await _userManager.GetUserChatStats(Message.ReplyToMessage.From.Id, ChatId);

            // If user not exists in db then ignore
            if (userStats == null)
                return;

            userStats.Karma++;
            await _userManager.UpdateUserChatStats(userStats);

            string replyText = string.Format(Resources.KarmaIncrease, userStats.User.UserMention, userStats.Karma);

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^-$")]
        public async Task DecreaseKarma()
        {
            // OOh, look at here, is this code dUpLiCatIOn???
            // Ignore message forwards
            if (Message.ForwardFrom != null || Message.ForwardFromChat != null)
                return;

            // Filter only reply to other user, ignore bots
            if (Message.ReplyToMessage == null || Message.ReplyToMessage.From!.Id == User.Id || Message.ReplyToMessage.From.IsBot)
                return;

            UserChatStats userStats = await _userManager.GetUserChatStats(Message.ReplyToMessage.From.Id, ChatId);

            // If user not exists in db then ignore
            if (userStats == null)
                return;

            userStats.Karma--;
            await _userManager.UpdateUserChatStats(userStats);

            string replyText = string.Format(Resources.KarmaDecrease, userStats.User.UserMention, userStats.Karma);

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("(^топ$|^top$)")]
        public async Task Top()
        {
            // Prevent for top spamming (1 top message per all chats, needs to rework)
            var timeout = TimeSpan.FromMilliseconds(50);
            bool lockTaken = false;

            try
            {
                Monitor.TryEnter(_topLocked, timeout, ref lockTaken);
                if (lockTaken)
                {
                    // Get all users
                    var users = _userManager.GetAllUsersChatStats(ChatId).Result;

                    String replyText = $"*{Resources.AccountsInTheChat} {users.Count()}*\n\n";
                    replyText += $"{Resources.TopChatKarma}\n";

                    users.OrderByDescending(x => x.Karma)
                        .Take(5)
                        .ToList()
                        .ForEach(x =>
                        {
                            float karmaPercent = 0;
                            if (x.Karma > 0 && x.TotalMessages > 0)
                            {
                                karmaPercent = (float)x.Karma * 100 / x.TotalMessages;
                            }

                            replyText += $"`{x.User.UserName}` - {Resources.Karma} `{x.Karma} ({karmaPercent}%)`\n";
                        });

                    var topMinus3Users = users.OrderBy(x => x.Karma)
                        .Where(x => x.Karma < 0)
                        .Take(3)
                        .OrderByDescending(x => x.Karma)
                        .ToList();

                    if (topMinus3Users.Any())
                    {
                        replyText += $"\n{Resources.TopChatNegativeKarma}\n";

                        topMinus3Users.ForEach(x =>
                        {
                            float karmaPercent = 0;
                            if (x.Karma > 0 && x.TotalMessages > 0)
                            {
                                karmaPercent = (float)x.Karma * 100 / x.TotalMessages;
                            }

                            replyText += $"`{x.User.UserName}` - {Resources.Karma} `{x.Karma} ({karmaPercent}%)`\n";
                        });
                    }

                    replyText += $"\n{Resources.TopChatActive}\n";

                    users.OrderByDescending(x => x.TotalMessages)
                        .Take(5)
                        .ToList()
                        .ForEach(x =>
                        {
                            replyText += $"`{x.User.UserName}` - {Resources.Messages} `{x.TotalMessages}`\n";
                        });

                    replyText += $"\n{Resources.TopChatEmotionals}\n";

                    users.OrderByDescending(x => x.TotalBadWords)
                        .Take(5)
                        .ToList()
                        .ForEach(x =>
                        {
                            float BadWordsPercent = 0;
                            if (x.TotalBadWords > 0 && x.TotalMessages > 0)
                            {
                                BadWordsPercent = (float)x.TotalBadWords * 100 / x.TotalMessages;
                            }

                            replyText += $"`{x.User.UserName}` - {Resources.BadWords} `{x.TotalBadWords} ({BadWordsPercent}%)`\n";
                        });

                    var topWarnsUsers = users.OrderByDescending(x => x.Warns)
                        .Where(x => x.Warns > 0)
                        .Take(5)
                        .ToList();

                    if (topWarnsUsers.Any())
                    {
                        replyText += $"\n{Resources.TopChatWarns}\n";

                        topWarnsUsers.ForEach(x =>
                        {
                            replyText += $"`{x.User.UserName}` - {Resources.Warns} `{x.Warns}`\n";
                        });
                    }

                    Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

                    Task.Delay(30 * 1000).Wait();

                    BotClient.TryDeleteMessage(msg).Wait();
                    BotClient.TryDeleteMessage(Message).Wait();
                }
                else // Top list is already exists, just remove top command message
                {
                    await BotClient.TryDeleteMessage(Message);
                }
            }
            finally
            {
                // Ensure that the lock is released.
                if (lockTaken)
                {
                    Monitor.Exit(_topLocked);
                }
            }
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/tickets$")]
        public async Task TicketList()
        {
            String replyText = "";

            // Сheck if user have rights to scan
            ChatMember usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From!.Id);
            if (usrSenderRights.Status != ChatMemberStatus.Administrator && usrSenderRights.Status != ChatMemberStatus.Creator)
            {
                replyText = Resources.OnlyAdminsAreAllowed;
            }
            else
            {
                TicketManager ticketManager = new TicketManager();
                replyText = await ticketManager.GetChatTickets(ChatId);
            }

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, parseMode: ParseMode.Markdown);
            await BotClient.TryDeleteMessage(Message);

            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/addticket")]
        public async Task AddTicket()
        {
            String replyText = "";

            // Сheck if user have rights to scan
            ChatMember usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From!.Id);
            if (usrSenderRights.Status != ChatMemberStatus.Administrator && usrSenderRights.Status != ChatMemberStatus.Creator)
            {
                replyText = Resources.OnlyAdminsAreAllowed;
            }
            else
            {
                // Parse message
                string ticketDescription = Message!.Text!
                    .Replace("/addticket", "")
                    .Trim();

                if (ticketDescription != "")
                {
                    TicketManager ticketManager = new TicketManager();
                    await ticketManager.AddTicket(ChatId, ticketDescription);
                    replyText = string.Format(Resources.TicketAdded, ticketDescription);
                }
                else
                {
                    replyText = Resources.NeedToDefineTicket;
                }
            }

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, parseMode: ParseMode.Markdown);

            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(Message);
            await BotClient.TryDeleteMessage(msg);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/removeticket")]
        public async Task RemoveTicket()
        {
            String replyText = "";

            // Сheck if user have rights to scan
            ChatMember usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From!.Id);
            if (usrSenderRights.Status != ChatMemberStatus.Administrator && usrSenderRights.Status != ChatMemberStatus.Creator)
            {
                replyText = Resources.OnlyAdminsAreAllowed;
            }
            else
            {
                // Parse message
                string ticketIdString = Message!.Text!
                    .Replace("/removeticket", "")
                    .Trim();

                if (ticketIdString == "")
                {
                    replyText = Resources.WhereIsTicketNumber;
                }
                else
                {
                    if (long.TryParse(ticketIdString, out long ticketId))
                    {
                        TicketManager ticketManager = new TicketManager();

                        bool removeResult = await ticketManager.RemoveTicket(ChatId, ticketId);

                        if (removeResult)
                            replyText = string.Format(Resources.TicketDeleted, ticketId);
                        else
                            replyText = Resources.HackerInTheChat;
                    }
                    else
                    {
                        replyText = Resources.AreYouThinkImThatDumb;
                    }
                }
            }

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, parseMode: ParseMode.Markdown);

            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(Message);
            await BotClient.TryDeleteMessage(msg);
        }

        [MessageReaction(ChatAction.UploadPhoto)]
        [MessageHandler("(^/cat$|^cat$|^кіт$|^кицька$)")]
        public async Task Cat()
        {
            string carUrl = await _catService.GetRandomCatImageUrl();

            if (carUrl == null)
            {
                Message msg = await BotClient.SendTextMessageAsync(chatId: ChatId, text: Resources.GoneAway, replyToMessageId: Message.MessageId);

                await Task.Delay(30 * 1000);
                await BotClient.TryDeleteMessage(msg);
                await BotClient.TryDeleteMessage(Message);

                return;
            }

            // Random cat gender
            List<string> variants = Resources.RandomCatGenders
                .Split("|")
                .PickRandom(2)
                .ToList();

            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("Кіт", $"print|{variants[0]}"),
                InlineKeyboardButton.WithCallbackData("Кітесса", $"print|{variants[1]}"),
            });

            await BotClient.SendPhotoAsync(chatId: ChatId, photo: carUrl, replyToMessageId: Message.MessageId, replyMarkup: keyboard);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^кіт ")]
        public async Task ChatGptAsk()
        {
            string inputMessageTest = Message!.Text!.Replace("кіт ", "").Replace("Кіт ", "");
            string returnMessage = ":)";

            if (String.IsNullOrEmpty(inputMessageTest))
            {
                returnMessage = Resources.Empty;
            }

            try
            {
                OpenAIAPI api = new OpenAIAPI(new APIAuthentication(Environment.GetEnvironmentVariable("RUDEBOT_OPENAI_API_KEY")!), engine: new Engine(Resources.GPTModel));

                var result = await api.Completions.CreateCompletionAsync(inputMessageTest, max_tokens: 50, temperature: 0.0);
                returnMessage = result.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                returnMessage = Resources.OopsIDidntAgain;
            }

            await BotClient.SendTextMessageAsync(ChatId, returnMessage, replyToMessageId: Message.MessageId);
        }

        [MessageTypeFilter(MessageType.Text)]
        public async Task MessageTrigger()
        {
            if (Message.Text != null)
            {
                string replyText = "";
                Random random = new Random();

                if ((Message?.ReplyToMessage?.From?.Id == BotClient.BotId) || (random.Next(1, 1000) > 985))
                {
                    var advices = AdvicesService.GetWords();
                    replyText = advices.PickRandom();
                }

                if (!string.IsNullOrEmpty(replyText))
                {
                    bool isReply = (random.Next(100) > 50);
                    await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: isReply ? Message!.MessageId : null);
                }
            }
        }
    }
}
