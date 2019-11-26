using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HumanService.Announcement;
using System.Threading.Tasks;

namespace HumanService
{
  [Group("settings"), Alias("s")]
  [RequireContext(ContextType.Guild)]
  public partial class Settings : ModuleBase<SocketCommandContext>
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
        _ = Context.Message.DeleteAsync();
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
        _ = Context.Message.DeleteAsync();
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
        _ = Context.Message.DeleteAsync();
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
        _ = Context.Message.DeleteAsync();
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

    #region Private functions

    private static string SuccessMsg(string msg) => ":white_check_mark: " + msg;
    private static string FailMsg(string msg) => ":negative_squared_cross_mark: " + msg;

    #endregion
  }
}
