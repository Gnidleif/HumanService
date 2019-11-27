using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace HumanService.Administration
{
  [RequireContext(ContextType.Guild)]
  public class Administration : ModuleBase<SocketCommandContext>
  {
    #region Owner

    [Command("owner"), Alias("own")]
    public async Task OwnMessage()
    {
      var cfg = new Config();
      if (Context.User.Id == cfg.Bot.Owner)
      {
        _ = UserExtensions.SendMessageAsync(Context.User, "Hello, master");
        _ = Logger.Instance.Write(new LogMessage("Master requested owner function", "Administration:OwnMessage"));
      }
      await Task.CompletedTask;
    }

    #endregion

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
        _ = Logger.Instance.Write(new LogException(e, "Administration:KickUser", LogSeverity.Error));
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
        _ = Logger.Instance.Write(new LogException(e, "Administration:BanUser", LogSeverity.Error));
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

      _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, $"Purge({count}) at {Context.Channel.Name}", "Administration:PurgeChannel"));
      var m = await SuccessReply($"Successfully removed up to {count} messages");
      await Task.Delay(5000);
      _ = m.DeleteAsync();
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

      _ = ReplyAsync("", false, embed.Build());
      _ = UserExtensions.SendMessageAsync(user, $"You have been {action} from {Context.Guild.Name}, reason: {reason}");
      _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, $"{user.Username} was {action}", "Administration:AdminReply"));

      await Task.CompletedTask;
    }

    private async Task<IUserMessage> SuccessReply(string msg) => await ReplyAsync($":white_check_mark: {msg}");

    private async Task<IUserMessage> FailReply(string msg) => await UserExtensions.SendMessageAsync(Context.User, $":negative_squared_cross_mark: {msg}");

    #endregion
  }
}
