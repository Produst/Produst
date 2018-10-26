﻿using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using WarBot.Core.Dialogs;
using WarBot.Core.Helper;
using WarBot.Core.ModuleType;
using System.Linq;
using WarBot.Core;

namespace WarBot.Modules.Dialogs
{
    public class SetupDialog : SocketGuildDialogContextBase
    {
        public SetupDialog(GuildCommandContext Context)
            : base(Context) { }


        private SetupStep CurrentStep = SetupStep.Initial;
        public async override Task ProcessMessage(SocketUserMessage input)
        {
            string Message = input.Content.Trim();
            string rawMsg = input.Content.Trim().ToLowerInvariant();

            if (rawMsg == "stop" || rawMsg == "cancel")
            {
                await Send("Setup aborted.");
                await this.Bot.CloseDialog(this);
                return;
            }
            else if (rawMsg == "back" || rawMsg == "go back")
            {
                //If the user said back, return to the previous step.
                await PreviousStep();
                return;
            }

            //Parse out the differant options we will need.
            bool Skip = rawMsg == "skip";
            bool? Bool = rawMsg.ParseBool();
            SocketTextChannel CH = input.MentionedChannels.OfType<SocketTextChannel>().FirstOrDefault();
            SocketRole ROLE = input.MentionedRoles.FirstOrDefault();


            //ToDo - Submitted a issue for dehumanizer, to see if this "logic" can be offloaded to their library.
            const string msg_BoolParseFailed = "I did not reconize that message. You may say yes, no, true, false, y or n.\r\n";
            string msg_ChannelExpected = $"I expected a channel. Please tag the channel like this: {this.Channel.Mention}\r\n";
            string msg_RoleExpected = $"I expected a role. Please tag the role like this:\r\n {this.Guild.CurrentUser.Roles.FirstOrDefault(o => o.IsMentionable)?.Mention ?? "@MyRole"}";



            switch (CurrentStep)
            {
                case SetupStep.Initial:
                    await NextStep();
                    break;
                case SetupStep.WarBot_Prefix:
                    if (Skip)
                        Config.Prefix = "bot,";
                    else
                        Config.Prefix = Message;

                    await NextStep($"My prefix has been set to '{Config.Prefix}'");
                    break;
                case SetupStep.Greeting_Should_Greet:
                    if (Bool == true)
                        await NextStep();
                    else if (Bool == false || Skip)
                    {
                        this.Config.Notifications.NewUserGreeting = null;
                        this.Config.SetGuildChannel(Core.WarBotChannelType.CH_New_Users, null);
                        await SkipStep("I will not send new user greetings.");
                    }
                    else
                        await Send(msg_BoolParseFailed);
                    break;
                case SetupStep.Greeting_Channel_NewUsers:
                    if (Skip)
                    {
                        Config.SetGuildChannel(Core.WarBotChannelType.CH_New_Users, null);
                        await SkipStep("I will not send new user greetings.");
                    }
                    else if (CH != null)
                    {
                        Config.SetGuildChannel(Core.WarBotChannelType.CH_New_Users, CH);
                        await NextStep($"New user greetings will go to {CH.Mention}");
                    }
                    else
                        await Send(msg_ChannelExpected);
                    break;
                case SetupStep.Greeting_Message:
                    if (Skip)
                    {
                        Config.SetGuildChannel(Core.WarBotChannelType.CH_New_Users, null);
                        await SkipStep("I will not send new user greetings.");
                    }
                    else
                    {
                        Config.Notifications.NewUserGreeting = Message;
                        await NextStep("New users joining the server will receive this message:\r" +
                            $"\n{User.Mention}, {Message}\r" +
                            $"\nIn channel {Config.GetGuildChannel(Core.WarBotChannelType.CH_New_Users).Mention}");
                    }
                    break;
                case SetupStep.Channel_Updates:
                    if (Skip)
                    {
                        Config.SetGuildChannel(Core.WarBotChannelType.CH_WarBot_Updates, null);
                        Config.Notifications.SendUpdateMessage = false;
                        await SkipStep("I will not send notifications when I am updated.");
                    }
                    else if (CH != null)
                    {
                        Config.Notifications.SendUpdateMessage = true;
                        Config.SetGuildChannel(Core.WarBotChannelType.CH_WarBot_Updates, CH);
                        await NextStep($"My update notifications will be sent to {CH.Mention}.");
                    }
                    else
                        await Send(msg_ChannelExpected);

                    break;
                case SetupStep.Channel_Officers:
                    if (Skip)
                    {
                        Config.SetGuildChannel(Core.WarBotChannelType.CH_Officers, null);
                        await SkipStep("I will not send any officer related messages or errors.");
                    }
                    else if (CH != null)
                    {
                        Config.SetGuildChannel(Core.WarBotChannelType.CH_Officers, CH);
                        await NextStep($"My officer-related messages and errors will be sent to {CH.Mention}.");
                    }
                    else
                        await Send(msg_ChannelExpected);
                    break;
                case SetupStep.Channel_WAR:
                    if (Skip || Bool == false)
                    {
                        Config.SetGuildChannel(Core.WarBotChannelType.CH_WAR_Announcements, null);
                        Config.Notifications.WarPrepStarted = false;
                        Config.Notifications.WarPrepEnding = false;
                        Config.Notifications.WarStarted = false;
                        await SkipStep("I will not send any type of announcements for hustle castle clan wars.\r" +
                            "\nYou may enable this feature later if you wish.");
                    }
                    else if (CH != null)
                    {
                        Config.SetGuildChannel(Core.WarBotChannelType.CH_WAR_Announcements, CH);
                        await NextStep($"My war announcements will be sent to {CH.Mention}.");
                    }
                    else
                        await Send(msg_ChannelExpected);
                    break;
                case SetupStep.WAR_SendPrepStarted:
                    if (Bool == true)
                    {
                        this.Config.Notifications.WarPrepStarted = true;
                        await NextStep();
                    }
                    else if (Bool == false || Skip)
                    {
                        this.Config.Notifications.WarPrepStarted = false;
                        await SkipStep("I will not send notifications when war prep starts.");
                    }
                    else
                        await Send(msg_BoolParseFailed);
                    break;
                case SetupStep.WAR_PrepStartedMessage:
                    if (Skip)
                    {
                        this.Config.Notifications.WarPrepStartedMessage = null;
                        await SkipStep("I will use my default notification when war prep starts.");
                    }
                    else
                    {
                        this.Config.Notifications.WarPrepStartedMessage = Message;
                        await NextStep("The war prep started message has been set to:\r" +
                            $"\n{Message}");
                    }
                    break;
                //War prep ending 
                case SetupStep.WAR_SendPrepEnding:
                    if (Bool == true)
                    {
                        this.Config.Notifications.WarPrepEnding = true;
                        await NextStep();
                    }
                    else if (Bool == false || Skip)
                    {
                        this.Config.Notifications.WarPrepEnding = false;
                        this.Config.Notifications.WarPrepEndingMessage = null;
                        await SkipStep("I will not send notifications for war prep ending.");
                    }
                    //Failed to parse a boolean.
                    else
                        await Send(msg_BoolParseFailed);
                    break;
                case SetupStep.WAR_PrepEndingMessage:
                    if (Skip)
                    {
                        this.Config.Notifications.WarPrepEndingMessage = null;
                        await NextStep("I will use my default notification when war prep is ending.");
                    }
                    else
                    {
                        this.Config.Notifications.WarPrepEndingMessage = Message;
                        await NextStep("The war prep ending message has been set to:\r" +
                            $"\n{Message}");
                    }
                    break;
                //War Started
                case SetupStep.WAR_SendWarStarted:
                    if (Bool == true)
                    {
                        this.Config.Notifications.WarStarted = true;
                        await NextStep();
                    }
                    else if (Bool == false || Skip)
                    {
                        this.Config.Notifications.WarStarted = false;
                        this.Config.Notifications.WarStartedMessage = null;
                        await SkipStep("I will not send a notification when clan wars start.");
                    }
                    else
                        await Send(msg_BoolParseFailed);
                    break;
                case SetupStep.WAR_WarStartedMessage:
                    if (Skip)
                    {
                        this.Config.Notifications.WarStartedMessage = null;
                        await SkipStep("I will use my default notification after war has started.");
                    }
                    else
                    {
                        this.Config.Notifications.WarStartedMessage = Message;
                        await NextStep("The war started message has been set to:\r" +
                            $"\n{Message}");
                    }
                    break;
                case SetupStep.Should_Set_Roles:
                    if (Bool == true)
                        await NextStep();
                    else if (Bool == false || Skip)
                    {
                        this.Config.ClearAllRoles();
                        await SkipStep("I will not perform any role-based management or tagging.");
                    }
                    else
                        await Send(msg_BoolParseFailed);
                    break;

                //All of these steps share the same logic.
                case SetupStep.Role_Guest:
                case SetupStep.Role_Member:
                case SetupStep.Role_Officer:
                case SetupStep.Role_Leader:
                case SetupStep.Role_ServerAdmin:
                    var role = GetRoleFromStep(CurrentStep);
                    if (Skip)
                    {
                        Config.SetGuildRole(role, null);
                        await SkipStep();
                    }
                    else if (ROLE != null)
                    {
                        Config.SetGuildRole(role, ROLE);
                        await NextStep($"Role {role.ToString()} has been assigned to discord role {(ROLE.IsMentionable ? ROLE.Mention : ROLE.Name)}");
                    }
                    else
                        await Send(msg_RoleExpected);
                    break;
                case SetupStep.Set_Website:
                    if (Skip)
                    {
                        Config.Website = null;
                        await SkipStep("No website will be configured.");
                    }
                    else
                    {
                        Config.Website = Message;
                        await NextStep("The website message has been set to:\r" +
                            $"\n{Message}");
                    }
                    break;
                case SetupStep.Set_Loot:
                    if (Skip)
                    {
                        Config.Loot = null;
                        await SkipStep("No loot message will be configured.");
                    }
                    else
                    {
                        Config.Loot = Message;
                        await NextStep("The Loot message has been set to:\r" +
                            $"\n{Message}");
                    }
                    break;
                default:
                    throw new Exception("Unhandled case.");
            }
        }


        private async Task StartStep(SetupStep step)
        {
            CurrentStep = step;
            const string suffix = "\n\r\n*You may always say 'skip' to continue to the next step, 'back' to return to the previous step, or 'stop' to cancel this dialog.*\r";
            switch (step)
            {
                case SetupStep.Initial:
                    await Send("Welcome to the guild setup dialog.\r" +
                        "\nI will walk you through the process of configuring me, by asking simple questions.");
                    return;

                case SetupStep.WarBot_Prefix:
                    await Send("Lets start by asking what my prefix should be.\r" +
                        $"\nYou also may always address me by tagging me, like this:\r" +
                        $"\n**{this.Bot.Client.CurrentUser.Mention}, help**\r" +
                        $"\nWhat prefix should I use?" +
                        suffix);
                    return;
                case SetupStep.Greeting_Should_Greet:
                    await Send("Would you like me to greet new users joining your server?\r" +
                        suffix);
                    break;
                case SetupStep.Greeting_Channel_NewUsers:
                    await Send("Which channel would you like me to use for greeting new users?\r" +
                        "\nIt is recommend the new users can see the new channel, but, you can set this to a private channel to notify your officers.\r" +
                        $"\nPlease tag the channel like this: {this.Channel.Mention}");
                    break;
                case SetupStep.Greeting_Message:
                    await Send("What message would you like me to send to new users?");
                    break;
                case SetupStep.Channel_Updates:
                    await Send("Occasionally, my developer will add significant new features to me.\r" +
                        "\nWould you like to receive those updates in a channel?\r" +
                        "\nIf so, please tell me which channel to send update notices to. If you do not want this, say 'No'.\r" +
                        suffix);
                    break;
                case SetupStep.Channel_Officers:
                    await Send("Occasionally, I need to communicate with the clan's leadership.\r" +
                        "\nWould you like to receive those updates in a channel? (They are not very frequent.)\r" +
                        "\nIf so, please tell me which channel to use. If you do not want this, say 'No'.\r" +
                        suffix);
                    break;
                case SetupStep.Channel_WAR:
                    await Send("I assume you invited me to your server, for the purpose of alerting for Hustle Castle War events.\r" +
                        "\nPlease let me know which channel I should send war announcments to.\r" +
                        "\nIf you say 'No' or 'Skip', I will disable all war-related announcements.");
                    break;
                case SetupStep.WAR_SendPrepStarted:
                    await Send("Would you like me to send an announcement when the war preperation peroid starts?" +
                        "\r\nYes or No?");
                    break;
                case SetupStep.WAR_PrepStartedMessage:
                    await Send("What message would you like for me to send to members when the war preperation peroid starts?" +
                        "\r\nYou may 'skip' to use a default message.");
                    break;
                case SetupStep.WAR_SendPrepEnding:
                    await Send("Would you like me to send an announcement 15 minutes before the war starts?" +
                        "\r\nYes or No?");
                    break;
                case SetupStep.WAR_PrepEndingMessage:
                    await Send("What message would you like for me to send before the war starts?" +
                        "\r\nYou may 'skip' to use a default message.");
                    break;
                case SetupStep.WAR_SendWarStarted:
                    await Send("Would you like me to send an announcement when the war starts?" +
                        "\r\nYes or No?");
                    break;
                case SetupStep.WAR_WarStartedMessage:
                    await Send("What message would you like for me to send when the war starts?" +
                        "\r\nYou may 'skip' to use a default message.");
                    break;
                case SetupStep.Should_Set_Roles:
                    await Send("Would you like me to assist you with managing the roles of your discord server?\r" +
                        "\nI can help by promoting users, demoting users, and setting users to specific roles\r" +
                        "\nAs well, many of my functions requires a user to have a specific role\r" +
                        "\nPlease say yes or no.");
                    break;
                case SetupStep.Role_Guest:
                    await Send("Which role would you like to utilize for guests?\r" +
                        "\nTypically, users in this role will not have access to many of the protected channels, or features of mine.\r" +
                        "\nPlease tag a role, like so: @Guests, or say 'skip'");
                    break;
                case SetupStep.Role_Member:
                    await Send("Which role would you like to use for members?\r" +
                        "\nThese users will have access to basic commands.\r" +
                         "\nPlease tag a role, like so: @Members, or say 'skip'");
                    break;
                case SetupStep.Role_Officer:
                    await Send("Which role would you like to use for officers?\r" +
                        "\nThese users will have access to a few user management commands, and will be able to set users to roles less then officer\r" +
                         "\nPlease tag a role, like so: @Officers, or say 'skip'");
                    break;
                case SetupStep.Role_Leader:
                    await Send("Which role would you like to use for Leaders?\r" +
                        "\nThese users will have access to nearly all of my features around user and clan management.\r" +
                         "\nPlease tag a role, like so: @Leaders, or say 'skip'");
                    break;
                case SetupStep.Role_ServerAdmin:
                    await Send("Which role would you like to use for Server Admins?\r" +
                        "\nThese users will have access to all of my commands. It is not required that you set this role, as I can detect users who have administrative permissions.\r" +
                         "\nPlease tag a role, like so: @Admins, or say 'skip'");
                    break;
                case SetupStep.Set_Website:
                    await Send("I can assist in directing users to a website, or message you specify when somebody says, 'bot, website'\r" +
                        "If you would like to use this feature, please tell me the message to send. Else, say 'skip'.");
                    break;
                case SetupStep.Set_Loot:
                    await Send("I can assist in pointing users to how your loot is managed when somebody says, 'bot, loot'.\r" +
                        "If you would like to use this feature, please tell me the message to send. Else, say 'skip'.");
                    break;
                case SetupStep.Done:
                    {
                        await Send("You have successfully completed my setup wizard.\r" +
                            "\nYou may always type 'bot, setup' to re-run this wizard, or 'bot, help' to show your available commands.\r" +
                            "\nIf you run into any issues, you may submit an issue at https://github.com/XtremeOwnage/WarBot or, join the support server for me.\r" +
                            "\nThanks for trusting WarBOT with all of your needs!");

                        await Config.SaveConfig();

                        await Bot.CloseDialog(this);
                    }
                    break;
            }
        }
        public async override Task OnCreated()
        {
            await StartStep(SetupStep.Initial);
            await StartStep(SetupStep.WarBot_Prefix);
        }
        public async override Task OnClosed()
        {
            await this.Channel.SendMessageAsync("The guild setup dialog has been closed.");
        }


        enum SetupStep
        {
            NULL,

            [Step(Initial, WarBot_Prefix)]
            Initial,

            //Setup the prefix for the bot.
            [Step(WarBot_Prefix, Greeting_Should_Greet)]
            WarBot_Prefix,

            [Step(WarBot_Prefix, Greeting_Channel_NewUsers, Channel_Updates)]
            //Determine if we should greet new users.
            Greeting_Should_Greet,

            [Step(Greeting_Should_Greet, Greeting_Message, Channel_Updates)]
            Greeting_Channel_NewUsers,

            [Step(Greeting_Channel_NewUsers, Channel_Updates)]
            Greeting_Message,

            //Configure various channels
            [Step(Greeting_Should_Greet, Channel_Officers)]
            Channel_Updates,

            [Step(Channel_Updates, Channel_WAR)]
            Channel_Officers,

            //Hustle Castle - War Related Settings
            [Step(Channel_Officers, WAR_SendPrepStarted, Should_Set_Roles)]
            Channel_WAR,
            [Step(Channel_WAR, WAR_PrepStartedMessage, WAR_SendPrepEnding)]
            WAR_SendPrepStarted,
            [Step(WAR_SendPrepStarted, WAR_SendPrepEnding)]
            WAR_PrepStartedMessage,
            [Step(WAR_SendPrepStarted, WAR_PrepEndingMessage, WAR_SendWarStarted)]
            WAR_SendPrepEnding,
            [Step(WAR_SendPrepEnding, WAR_SendWarStarted)]
            WAR_PrepEndingMessage,
            [Step(WAR_SendPrepEnding, WAR_WarStartedMessage, Should_Set_Roles)]
            WAR_SendWarStarted,
            [Step(WAR_SendWarStarted, Should_Set_Roles)]
            WAR_WarStartedMessage,


            //Roles    
            [Step(WarBot_Prefix, Role_Guest, Set_Website)]
            Should_Set_Roles,
            [Step(Should_Set_Roles, Role_Member)]
            Role_Guest,
            [Step(Role_Guest, Role_Officer)]
            Role_Member,
            [Step(Role_Member, Role_Leader)]
            Role_Officer,
            [Step(Role_Officer, Role_ServerAdmin)]
            Role_Leader,
            [Step(Role_Leader, Set_Website)]
            Role_ServerAdmin,

            [Step(Should_Set_Roles, Set_Loot)]
            Set_Website,

            [Step(Set_Website, Done)]
            Set_Loot,

            [Step(Set_Loot, Done)]
            Done,
        }

        #region Various other related pieces of code, helpers and shortcuts.
        private RoleLevel GetRoleFromStep(SetupStep Step)
        {
            switch (Step)
            {
                case SetupStep.Role_Guest:
                    return RoleLevel.Guest;
                case SetupStep.Role_Member:
                    return RoleLevel.Member;
                case SetupStep.Role_Officer:
                    return RoleLevel.Officer;
                case SetupStep.Role_Leader:
                    return RoleLevel.Leader;
                case SetupStep.Role_ServerAdmin:
                    return RoleLevel.ServerAdmin;
                default:
                    throw new Exception("Bad input. That step does not have an associated role.");
            }

        }
        private async Task NextStep(string Message = null)
        {
            if (Message != null)
                await Send(Message);
            SetupStep NextStep = GetStep(CurrentStep).NextStep;
            await StartStep(NextStep);
        }
        private async Task SkipStep(string Message = null)
        {
            if (Message != null)
                await Send(Message);
            SetupStep SkipStep = GetStep(CurrentStep).SkipStep;
            await StartStep(SkipStep);
        }
        private async Task PreviousStep()
        {
            SetupStep PrevStep = GetStep(CurrentStep).PreviousStep;
            await StartStep(PrevStep);
        }
        public async Task Send(string Message)
            => await this.Channel.SendMessageAsync(text: Message);
        StepAttribute GetStep(SetupStep Value)
        {
            var type = typeof(SetupStep);
            var name = Enum.GetName(type, Value);
            return type.GetField(name).GetCustomAttributes(false).OfType<StepAttribute>().SingleOrDefault();
        }
        class StepAttribute : Attribute
        {
            public SetupStep PreviousStep { get; set; }
            public SetupStep NextStep { get; set; }
            public SetupStep SkipStep { get; set; }
            public StepAttribute(SetupStep Previous, SetupStep Next, SetupStep Skip = SetupStep.NULL)
            {
                this.PreviousStep = Previous;
                this.NextStep = Next;
                this.SkipStep = Skip == SetupStep.NULL ? Next : Skip;
            }
        }
        #endregion
    }
}