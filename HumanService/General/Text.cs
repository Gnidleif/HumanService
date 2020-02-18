using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

      _ = Logger.Instance.WriteAsync(new LogCommand(Context.User, Context.Guild, result, "General:Text:ToSpongebob"));
      await ReplyAsync("", false, reply.Build());
    }

    [Command("8ball"), Alias("8b")]
    public async Task GetPrediction([Remainder] string message)
    {
      var checkQuestion = new Regex(@"[\w]+\?+$");
      if (!checkQuestion.IsMatch(message))
      {
        _ = UserExtensions.SendMessageAsync(Context.User, $"Invalid question format");
        return;
      }
      var rand = new Random(DateTime.UtcNow.Millisecond);
      var answers = new string[] {
        "It is certain",
        "It is decidedly so",
        "Without a doubt",
        "Yes - definitely",
        "You may rely on it",
        "As I see it, yes",
        "Most likely",
        "Outlook good",
        "Yes",
        "Signs point to yes",
        "Reply hazy, try again",
        "Ask again later",
        "Better not tell you now",
        "Cannot predict now",
        "Concentrate and ask again",
        "Don't count on it",
        "My reply is no",
        "My sources say no",
        "Outlook not so good",
        "Very doubtful"
      };
      var user = Context.User as IGuildUser;
      var embed = new EmbedBuilder();
      embed.WithAuthor(user.Nickname ?? user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
      embed.WithDescription("Prediction brought to you by the 8 ball gang");
      embed.AddField("Question", message);
      embed.AddField("Answer", answers[rand.Next(0, answers.Length)]);
      await ReplyAsync("", false, embed.Build());
    }
  }
}
