using Discord;
using Discord.Commands;
using HumanService.Timeout;
using System;
using System.Threading.Tasks;

namespace HumanService.Administration
{
  [RequireContext(ContextType.Guild)]
  public class Administration : ModuleBase<SocketCommandContext>
  {
    #region Basic

    [Command("kick"), Alias("k")]
    [RequireBotPermission(GuildPermission.KickMembers)]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task KickUser(IGuildUser user, [Remainder] string reason = "")
    {
      try
      {
        await user.KickAsync(reason);
      }
      catch (Discord.Net.HttpException e)
      {
        _ = Logger.Instance.WriteAsync(new LogException(e, "Administration:KickUser", LogSeverity.Error));
        _ = FailReply(e.Message);
        return;
      }

      _ = AdminReply("kicked", user, reason);
    }

    [Command("ban"), Alias("b")]
    [RequireBotPermission(GuildPermission.BanMembers)]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task BanUser(IGuildUser user, [Remainder] string reason = "")
    {
      try
      {
        await user.BanAsync(0, reason);
      }
      catch (Discord.Net.HttpException e)
      {
        _ = Logger.Instance.WriteAsync(new LogException(e, "Administration:BanUser", LogSeverity.Error));
        _ = FailReply(e.Message);
        return;
      }

      _ = AdminReply("banned", user, reason);
    }

    [Command("purge"), Alias("p")]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task PurgeChannel(uint count = 1)
    {
      var messagesTask = Context.Channel.GetMessagesAsync(Context.Message.Id, Direction.Before, (int)count).FlattenAsync();
      var ch = Context.Channel as ITextChannel;
      _ = ch.DeleteMessagesAsync(await messagesTask);

      _ = Logger.Instance.WriteAsync(new LogCommand(Context.User, Context.Guild, $"Purge({count}) at {Context.Channel.Name}", "Administration:PurgeChannel"));
      var m = await SuccessReply($"Successfully removed up to {count} messages");
      await Task.Delay(5000);
      _ = m.DeleteAsync();
    }

    #endregion

    #region Timeout

    [Group("timeout"), Alias("to")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class Timeout : ModuleBase<SocketCommandContext>
    {
      [Command]
      [RequireBotPermission(GuildPermission.ManageRoles)]
      public async Task TimeoutUser(IGuildUser user, uint minutes = 10, [Remainder] string reason = "")
      {
        if (minutes == 0)
        {
          minutes = (uint)new Random((int)Global.ToUnixTime()).Next(10, 5000);
        }

        try
        {
          await TimeoutResource.Instance.SetTimeout(user, minutes);
          if (user.VoiceChannel != null)
          {
            await user.ModifyAsync(x => x.Channel = null);
          }
        }
        catch (Exception e)
        {
          _ = Logger.Instance.WriteAsync(new LogException(e, "Administration:Timeout:TimeoutUser", LogSeverity.Error));
          _ = UserExtensions.SendMessageAsync(Context.User, e.Message);
          return;
        }

        var reply = new EmbedBuilder();
        reply.WithAuthor(user.Nickname ?? user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
        reply.WithDescription("User set on timeout");
        reply.AddField("Judge", Context.User.Username, true);
        reply.AddField("Minutes", minutes, true);
        if (!string.IsNullOrEmpty(reason))
        {
          reply.AddField("Reason", reason, true);
        }
        else
        {
          reason = "None specified";
        }

        _ = ReplyAsync("", false, reply.Build());
        _ = UserExtensions.SendMessageAsync(user, $"You have been set on timeout for {minutes} minutes by **{Context.User.Username}**, reason: **{reason}**");
        _ = Logger.Instance.WriteAsync(new LogCommand(Context.User, Context.Guild, $"{user.Username} set on timeout({minutes}), reason: {reason}", "Administration:SetTimeout"));
      }
    }

    #endregion

    #region Private functions

    private async Task AdminReply(string action, IGuildUser user, string reason)
    {
      var embed = new EmbedBuilder();
      embed.WithAuthor(user.Nickname ?? user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
      embed.WithDescription($"User {action}");
      embed.AddField("Judge", Context.User.Username, true);

      if (!string.IsNullOrEmpty(reason))
      {
        embed.AddField("Reason", reason, true);
      }
      else
      {
        reason = "None specified";
      }

      embed.WithFooter(Global.FormatTime());

      await ReplyAsync("", false, embed.Build());
      await UserExtensions.SendMessageAsync(user, $"You have been {action} from {Context.Guild.Name}, reason: {reason}");
      await Logger.Instance.WriteAsync(new LogCommand(Context.User, Context.Guild, $"{user.Username} was {action}", "Administration:AdminReply"));
    }

    private async Task<IUserMessage> SuccessReply(string msg) => await ReplyAsync($":white_check_mark: {msg}");

    private async Task<IUserMessage> FailReply(string msg) => await UserExtensions.SendMessageAsync(Context.User, $":negative_squared_cross_mark: {msg}");

    #endregion
  }
}
