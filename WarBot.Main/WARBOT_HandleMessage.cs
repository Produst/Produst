﻿using DiscordBotsList.Api;
using Discord.Net;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WarBot.Storage;
using Discord.Commands;
using System.Threading;
using WarBot.Core.Dialogs;

namespace WarBot
{
    public partial class WARBOT
    {
        private async Task Client_MessageReceived(SocketMessage socketMessage)
        {
            Interlocked.Increment(ref this.MessagesProcessed);
            try
            {
                var message = socketMessage as SocketUserMessage;

                //If this was a system message, ignore it.
                if (message == null)
                    return;

                //If the message is from a bot, ignore it.
                if (message.Author.IsBot)
                    return;

                //If the message is from me, ignore it.
                if (message.Author.Id == Client.CurrentUser.Id)
                    return;

                //If the message came from a logging channel, ignore it.
                if (Log.IsLoggingChannel(message.Channel.Id))
                    return;


                #region Parse out command from prefix.
                int argPos = 0;
                bool HasStringPrefix = message.HasStringPrefix("bot,", ref argPos, StringComparison.OrdinalIgnoreCase);
                bool HasBotPrefix = message.HasMentionPrefix(Client.CurrentUser, ref argPos);

                //Substring containing only the desired commands.
                string Msg = message.Content.Substring(argPos, message.Content.Length - argPos).Trim();
                #endregion

                //Start actual processing logic.              
                var UserChannelHash = SocketGuildDialogContextBase.GetHashCode(message.Channel, message.Author);
                //Check if there is an open dialog.
                //ToDo - If the hash logic is perfectly sound, we can remove the second check to improve performance.
                //This case, is outside of the channel type comparison, because a dialog can occur in many multiple channel types.
                if (this.Dialogs.ContainsKey(UserChannelHash) && this.Dialogs[UserChannelHash].InContext(message.Channel.Id, message.Author.Id))
                {
                    await this.Dialogs[UserChannelHash].ProcessMessage(message.Content);
                }
                else if (message.Channel is SocketTextChannel tch)
                {
                    //If the message was not to me, Ignore it.
                    if (!(HasStringPrefix || HasBotPrefix))
                        return;

                    var cfg = await this.GuildRepo.GetConfig(tch.Guild);

                    //Compares the guilds environment with the current processes environment.
                    if (!ShouldHandleMessage(cfg))
                        return;

                    //Load dynamic command context.
                    var context = new SocketCommandContext(Client, message);

                    var result = await commands.ExecuteAsync(context, Msg, services, MultiMatchHandling.Best);

                    await Log.ChatMessage(message, tch.Guild, result);
                }
                else if (message.Channel is SocketDMChannel dm)
                {
                    //Load dynamic command context.
                    await dm.SendMessageAsync("Sorry, I don't yet support direct messages. My developer is working on this functionality though.");
                    await dm.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                await Log.Error(null, ex);
            }
        }


    }
}
