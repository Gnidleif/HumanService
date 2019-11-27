using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HumanService.Announcement;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HumanService
{
  [Group("settings"), Alias("s")]
  [RequireContext(ContextType.Guild)]
  public class Settings : ModuleBase<SocketCommandContext>
  {

    #region Announcement

    [Group("announce"), Alias("an")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class Announce : ModuleBase<SocketCommandContext>
    {
      [Command]
      public async Task GetSettings()
      {
        var list = new AnnounceResource().GetAnnouncements(Context.Guild.Id);
        if (list == null)
        {
          var prefix = new Config().Bot.Guilds[Context.Guild.Id].Prefix;
          var msg = $"No announce settings configured for guild, run '{prefix}settings announce channel <channel id> to get started'";
          _ = UserExtensions.SendMessageAsync(Context.User, FailMsg(msg));
          _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, msg, "Settings:Announce:GetSettings", LogSeverity.Error));
          return;
        }

        var reply = new EmbedBuilder();
        var user = Context.User as SocketGuildUser;
        reply.WithAuthor(user.Nickname ?? user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
        foreach (var r in list)
        {
          if (r.Value != null)
          {
            reply.AddField(r.Key, r.Value, true);
          }
          else
          {
            reply.WithDescription($"**Channel**: <#{r.Key}>");
          }
        }

        _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, "Called", "Settings:Announce:GetSettings"));
        _ = ReplyAsync("", false, reply.Build());
        await Task.CompletedTask;
      }


      [Command("channel"), Alias("ch")]
      public async Task EditChannel(IChannel channel)
      {
        var announce = new AnnounceResource();
        var severity = LogSeverity.Info;
        string reply;
        if (announce.Push(Context.Guild.Id, channel.Id) || announce.SetChannel(Context.Guild.Id, channel.Id))
        {
          reply = SuccessMsg($"Successfully set guild announcement feature to channel <#{channel.Id}>");
          _ = ReplyAsync(reply);
          _ = announce.Save();
        }
        else
        {
          severity = LogSeverity.Error;
          reply = FailMsg($"Setting guild announcement channel failed");
          _ = UserExtensions.SendMessageAsync(Context.User, reply);
        }

        _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, reply, "Settings:Announce:EditChannel", severity));

        await Task.CompletedTask;
      }

      [Command("enable"), Alias("e")]
      public async Task EditState(string name, bool state)
      {
        var announce = new AnnounceResource();
        var severity = LogSeverity.Info;
        string reply;
        if (announce.SetState(Context.Guild.Id, name, state))
        {
          reply = SuccessMsg("Successfully " + (state ? "enabled" : "disabled") + $" the **{name}** announcement");
          _ = ReplyAsync(reply);
        }
        else
        {
          severity = LogSeverity.Error;
          reply = FailMsg($"Unable to set the state of the **{name}** announcement");
          _ = UserExtensions.SendMessageAsync(Context.User, reply);
        }

        _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, reply, "Settings:Announce:EditState", severity));

        await Task.CompletedTask;
      }

      [Command("message"), Alias("msg")]
      public async Task EditMsg(string name, [Remainder] string msg)
      {
        var announce = new AnnounceResource();
        var severity = LogSeverity.Info;
        string reply;
        if (announce.SetMessage(Context.Guild.Id, name, msg))
        {
          reply = SuccessMsg($"Successfully edited the **{name}** announcement message");
          _ = ReplyAsync(reply);
          _ = announce.Save();
        }
        else
        {
          severity = LogSeverity.Error;
          reply = FailMsg($"Unable to edit the **{name}** announcement message");
          _ = UserExtensions.SendMessageAsync(Context.User, reply);
        }

        _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, reply, "Settings:Announce:EditMsg", severity));

        await Task.CompletedTask;
      }
    }

    #endregion

    #region Welcome

    [Group("welcome"), Alias("w")]
    public class Welcome : ModuleBase<SocketCommandContext>
    {
      [Command]
      public async Task GetWelcome()
      {
        var cfg = new Config().Bot.Guilds[Context.Guild.Id].Welcome;
        var user = Context.User as SocketGuildUser;
        var reply = new EmbedBuilder();
        try
        {
          reply.WithAuthor(user.Nickname ?? user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
          reply.AddField("Enabled", $"**{cfg.Enabled}**", true);
          reply.AddField("Time", $"**{cfg.Time}**", true);
          reply.AddField("Role", "**" + (cfg.BaseRole > 1 ? Context.Guild.GetRole(cfg.BaseRole).Name : "Unspecified") + "**", true);
          reply.AddField("Message", cfg.Message);
          reply.WithFooter(Global.FormatTime());
        }
        catch (Exception e)
        {
          _ = Logger.Instance.Write(new LogException(e, "Settings:Welcome:GetWelcome", LogSeverity.Error));
          _ = UserExtensions.SendMessageAsync(Context.User, e.Message);
          return;
        }

        _ = ReplyAsync("", false, reply.Build());
        _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, "Called", "Settings:Welcome:GetWelcome"));
        await Task.CompletedTask;
      }

      [Command("switch"), Alias("s")]
      [RequireUserPermission(GuildPermission.Administrator)]
      public async Task SetState(bool state)
      {
        var cfg = new Config();
        cfg.Bot.Guilds[Context.Guild.Id].Welcome.Enabled = state;
        if (state && cfg.Bot.Guilds[Context.Guild.Id].Welcome.BaseRole <= 1)
        {
          var pos = 0;
          SocketRole baseRole = null;
          while (baseRole == null || baseRole.IsManaged || baseRole == Context.Guild.EveryoneRole)
          {
            baseRole = Context.Guild.Roles.Where(x => x.Position == pos).First();
            pos++;
          }
          cfg.Bot.Guilds[Context.Guild.Id].Welcome.BaseRole = baseRole.Id;
        }
        _ = cfg.Save();
        var msg = SuccessMsg("Successfully " + (state ? "enabled" : "disabled") + " welcome functionality!");
        _ = ReplyAsync(msg);
        _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, msg, "Settings:Welcome:SetState"));

        await Task.CompletedTask;
      }

      [Command("time"), Alias("t")]
      [RequireUserPermission(GuildPermission.Administrator)]
      public async Task SetTime(uint time)
      {
        var cfg = new Config();
        cfg.Bot.Guilds[Context.Guild.Id].Welcome.Time = time;
        if (time == 0)
        {
          cfg.Bot.Guilds[Context.Guild.Id].Welcome.Enabled = false;
        }
        _ = cfg.Save();

        var msg = SuccessMsg($"Successfully set welcome time to **{time} minutes**");
        _ = ReplyAsync(msg);
        _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, msg, "Settings:Welcome:SetTime"));

        await Task.CompletedTask;
      }

      [Command("baserole"), Alias("br")]
      [RequireUserPermission(GuildPermission.Administrator)]
      public async Task SetBaseRole(IRole role)
      {
        string reply;
        var severity = LogSeverity.Info;
        if (role.IsManaged || role == Context.Guild.EveryoneRole || Context.Guild.GetRole(role.Id) == null)
        {
          severity = LogSeverity.Error;
          reply = FailMsg($"**{role.Name}** is an invalid base role");
          _ = UserExtensions.SendMessageAsync(Context.User, reply);
        }
        else
        {
          var cfg = new Config();
          cfg.Bot.Guilds[Context.Guild.Id].Welcome.BaseRole = role.Id;
          _ = cfg.Save();
          reply = SuccessMsg($"Successfully set base role to **{role.Name}**");
          _ = ReplyAsync(reply);
        }

        _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, reply, "Settings:Welcome:SetBaseRole", severity));

        await Task.CompletedTask;
      }

      [Command("message"), Alias("m")]
      [RequireUserPermission(GuildPermission.Administrator)]
      public async Task SetMessage([Remainder] string message = "")
      {
        var cfg = new Config();
        cfg.Bot.Guilds[Context.Guild.Id].Welcome.Message = message;
        _ = cfg.Save();
        var reply = SuccessMsg(!string.IsNullOrEmpty(message) ? $"Successfully set new message to: '{message}'" : "A welcome message is no longer sent");

        _ = ReplyAsync(reply);
        _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, reply, "Settings:Welcome:SetMessage"));

        await Task.CompletedTask;
      }
    }

    #endregion

    #region Private functions

    private static string SuccessMsg(string msg) => ":white_check_mark: " + msg;
    private static string FailMsg(string msg) => ":negative_squared_cross_mark: " + msg;

    #endregion
  }
}
