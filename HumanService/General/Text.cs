using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HumanService.General
{
  public class Text : ModuleBase<SocketCommandContext>
  {
    [Command("spongebob"), Alias("sb")]
    public async Task ToSpongebob([Remainder] string message)
    {
      var rand = new Random(DateTime.UtcNow.Millisecond);
      var sb = new StringBuilder();
      foreach(var s in message.Select(x => x.ToString()))
      {
        sb.Append(rand.Next(0, 1 + 1) > 0 ? s.ToLower() : s.ToUpper());
      }

      var result = string.Join("", sb.ToString());
      var user = Context.User as SocketGuildUser;
      var reply = new EmbedBuilder();
      reply.WithAuthor(user.Nickname ?? user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
      reply.WithColor(247, 235, 98);
      reply.WithDescription(result);
      reply.WithFooter(Global.FormatTime());

      _ = Logger.Instance.Write(new LogCommand(Context.User, Context.Guild, result, "General:Text:ToSpongebob"));
      _ = ReplyAsync("", false, reply.Build());

      await Task.CompletedTask;
    }
  }
}
