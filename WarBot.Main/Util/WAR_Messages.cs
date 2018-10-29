using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WarBot.Core;

namespace WarBot.Util
{
    public class WAR_Messages
    {
        private IWARBOT bot;
        public WAR_Messages(IWARBOT Bot)
        {
            this.bot = Bot;
        }

        /// <summary>
        /// Determine is a guild is elected into a specific war.           
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="WarNo"></param>
        /// <returns></returns>
        private bool shouldSendSpecificWar(IGuildConfig cfg, byte WarNo)
        {
            if (WarNo == 1) return cfg.Notifications.War1Enabled;
            if (WarNo == 2) return cfg.Notifications.War2Enabled;
            if (WarNo == 3) return cfg.Notifications.War3Enabled;
            if (WarNo == 4) return cfg.Notifications.War4Enabled;

            else throw new ArgumentOutOfRangeException("There are only 4 wars. The value passed was not between 1 and 4.");
        }


        public async Task SendWarPrepStarted(byte WarNo)
        {
            var Errors = bot.GuildRepo
                .GetCachedConfigs()
                .Select(cfg => new Action(() =>
                {
                    //Guild has elected out for this notification.
                    if (!cfg.Notifications.WarPrepStarted)
                        return;
                    //Guild elected out of this specific war.
                    else if (!shouldSendSpecificWar(cfg, WarNo))
                        return;

                    //Send the message.
                    WarBot.Modules.MessageTemplates.WAR_Notifications.War_Prep_Started(cfg).Wait();
                })).executeParallel(bot.StopToken.Token, 1);

            foreach (Exception err in Errors)
            {
                await this.bot.Log.Error(null, err);
            }
        }

        public async Task SendWarPrepEnding(byte WarNo)
        {
            var Errors = bot.GuildRepo
                .GetCachedConfigs()
                .Select(cfg => new Action(() =>
                {
                    //Guild has elected out for this notification.
                    if (!cfg.Notifications.WarPrepEnding)
                        return;
                    //Guild elected out of this specific war.
                    else if (!shouldSendSpecificWar(cfg, WarNo))
                        return;

                    //Send the message.
                    WarBot.Modules.MessageTemplates.WAR_Notifications.War_Prep_Ending(cfg).Wait();
                })).executeParallel(bot.StopToken.Token, 1);

            foreach (Exception err in Errors)
            {
                await this.bot.Log.Error(null, err);
            }
        }

        public async Task SendWarStarted(byte WarNo)
        {
            var Errors = bot.GuildRepo
                  .GetCachedConfigs()
                  .Select(cfg => new Action(() =>
                  {
                      //Guild has elected out for this notification.
                      if (!cfg.Notifications.WarStarted)
                          return;
                      //Guild elected out of this specific war.
                      else if (!shouldSendSpecificWar(cfg, WarNo))
                          return;

                      //Send the message.
                      Modules.MessageTemplates.WAR_Notifications.War_Started(cfg).Wait();
                  })).executeParallel(bot.StopToken.Token, 1);

            foreach (Exception err in Errors)
            {
                await this.bot.Log.Error(null, err);
            }
        }

        public async Task SendPortalOpened()
        {
            var Errors = bot.GuildRepo
                 .GetCachedConfigs()
                 .Select(cfg => new Action(() =>
                 {
                     //Guild has elected out for this notification.
                     if (!cfg.Notifications.PortalEnabled)
                         return;

                     //Send the message.
                     Modules.MessageTemplates.Portal_Notifications.Portal_Opened(cfg).Wait();
                 })).executeParallel(bot.StopToken.Token, 1);

            foreach (Exception err in Errors)
            {
                await this.bot.Log.Error(null, err);
            }
        }
    }
}
