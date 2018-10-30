﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WarBot.Core;

namespace WarBot.Modules.MessageTemplates
{
    public static class WAR_Notifications
    {
        /// <summary>
        /// Sends an embed to the selected channel, if we have the proper permissions.
        /// Else- it will DM the owner of the guild.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="embed"></param>
        /// 
        /// <returns></returns>
        private static async Task sendWarMessage(IWARBOT bot, IGuildConfig cfg, Embed embed)
        {
            var ch = cfg.GetGuildChannel(WarBotChannelType.CH_WAR_Announcements) as SocketTextChannel;

            //If there is no channel configured, abort.
            if (ch == null)
                return;

            //If we can send to the WAR channel, and we have permissions.
            if (PermissionHelper.TestBotPermission(ch, ChannelPermission.SendMessages))
            {
                await ch.SendMessageAsync(embed: embed);
                return;
            }

            Console.WriteLine($"Missing SEND_PERMISSIONS for channel {ch.Name} for guild {cfg.Guild.Name}");
            StringBuilder sb = new StringBuilder()
                .AppendLine("ERROR: Missing Permissions")
                .AppendLine($"You are receiving this error, because I do not have the proper permissions to send the war notification to channel {ch.Name}.")
                .AppendLine("Please validate I have the 'SEND_MESSAGES' permission for the specified channel.");

            //Else, we don't have permissions to the WAR Channel. Send a notification to the officers channel.
            var och = cfg.GetGuildChannel(WarBotChannelType.CH_Officers) as SocketTextChannel;
            if (och != null && PermissionHelper.TestBotPermission(och, ChannelPermission.SendMessages))
            {
                await och.SendMessageAsync(sb.ToString());
                return;
            }





            //We don't have permissions to post to either channel. Lets try and DM the guild's owner... 
            try
            {
                var dm = await cfg.Guild.Owner.GetOrCreateDMChannelAsync();
                await dm.SendMessageAsync(sb.ToString());
                await dm.SendMessageAsync(embed: embed);
            }
            catch
            {
                //Well, out of options. Lets disable this channel for the guild.
                cfg.SetGuildChannel(WarBotChannelType.CH_WAR_Announcements, null);
                await cfg.SaveConfig();

                var error = new UnauthorizedAccessException("Missing permissions to send to WAR Channel. WAR messages disabled for this guild.");
                await bot.Log.Error(cfg.Guild, error, nameof(sendWarMessage));

            }

        }
        public static async Task War_Prep_Started(IGuildConfig cfg)
        {
            ///Determine the message to send.
            string Message = "";
            if (string.IsNullOrEmpty(cfg.Notifications.WarPrepStartedMessage))
                if (cfg.GetGuildRole(RoleLevel.Member).IsNotNull(out var role) && role.IsMentionable)
                    Message = $"{role.Mention}, WAR Placement has started! Please please your troops in the next two hours!";
                else
                    Message = "WAR Placement has started! Please please your troops in the next two hours!";
            else
                Message = cfg.Notifications.WarPrepStartedMessage;

            var eb = new EmbedBuilder()
                .WithTitle("WAR Prep Started")
                .WithDescription(Message);

            await sendWarMessage(cfg, eb.Build());
        }
        public static async Task War_Prep_Ending(IGuildConfig cfg)
        {
            ///Determine the message to send.
            string Message = "";
            if (string.IsNullOrEmpty(cfg.Notifications.WarPrepEndingMessage))
                if (cfg.GetGuildRole(RoleLevel.Member).IsNotNull(out var role) && role.IsMentionable)
                    Message = $"{role.Mention}, 15 minutes before war starts! Please place your troops if you have not done so already!!!";
                else
                    Message = "15 minutes before war starts! Please place your troops if you have not done so already!!!";
            else
                Message = cfg.Notifications.WarPrepEndingMessage;

            var eb = new EmbedBuilder()
                .WithTitle("WAR Prep Ending")
                .WithDescription(Message);

            await sendWarMessage(cfg, eb.Build());
        }
        public static async Task War_Started(IGuildConfig cfg)
        {
            ///Determine the message to send.
            string Message = "";
            if (string.IsNullOrEmpty(cfg.Notifications.WarStartedMessage))
                if (cfg.GetGuildRole(RoleLevel.Member).IsNotNull(out var role) && role.IsMentionable)
                    Message = $"{role.Mention}, WAR has started!";
                else
                    Message = "WAR has started!";
            else
                Message = cfg.Notifications.WarStartedMessage;

            var eb = new EmbedBuilder()
                .WithTitle("WAR Started")
                .WithDescription(Message);

            await sendWarMessage(cfg, eb.Build());
        }
    }
}
