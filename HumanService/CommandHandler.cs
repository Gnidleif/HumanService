using Discord.Commands;
using Discord.WebSocket;
using HumanService.TypeReaders;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HumanService
{

  public class CommandHandler
  {
    private CommandService Service { get; set; }

    public async Task Initialize()
    {
      Service = new CommandService(new CommandServiceConfig
      {
        CaseSensitiveCommands = false,
        DefaultRunMode = RunMode.Async,
        LogLevel = Discord.LogSeverity.Verbose,
      });

      Service.AddTypeReader(typeof(Regex), new RegexTypeReader());

      await Service.AddModulesAsync(Assembly.GetEntryAssembly(), null);

      Global.Client.MessageReceived += ClientMessageReceived;
    }

    private async Task ClientMessageReceived(SocketMessage arg)
    {
      var msg = arg as SocketUserMessage;
      if (msg == null)
      {
        return;
      }

      var ctx = new SocketCommandContext(Global.Client, msg);
      if (ctx.User.IsBot)
      {
        return;
      }

      var argPos = 0;
      var guildCfg = new Config().Bot.Guilds[ctx.Guild.Id];
      if (msg.HasStringPrefix(guildCfg.Prefix, ref argPos) || msg.HasMentionPrefix(Global.Client.CurrentUser, ref argPos))
      {
        var result = await Service.ExecuteAsync(ctx, argPos, null);
        if (!result.IsSuccess)
        {
          if (result.Error != CommandError.UnknownCommand)
          {
            _ = Logger.Instance.Write(new LogMessage($"Message: {msg.Content} | Error: {result.ErrorReason}", "CommandHandler:ClientMessageReceived", Discord.LogSeverity.Error));
          }
          _ = Discord.UserExtensions.SendMessageAsync(ctx.User, result.ErrorReason);
        }
      }
    }
  }
}
