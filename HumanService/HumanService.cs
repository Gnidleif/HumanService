using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace HumanService
{
  public partial class HumanService : ServiceBase
  {
    private CommandHandler Handler { get; set; } = new CommandHandler();

    public HumanService()
    {
      InitializeComponent();
    }

    protected override void OnStart(string[] args) => OnStartAsync().GetAwaiter().GetResult();

    protected async Task OnStartAsync()
    {
      Logger.Instance.Level = LogSeverity.Info;
      _ = Logger.Instance.Write(new LogMessage("Service started", "HumanService:OnStart"));

      Global.Client = new DiscordSocketClient(new DiscordSocketConfig
      {
        LogLevel = Logger.Instance.Level,
        ExclusiveBulkDelete = true,
      });

      Global.Client.Log += ClientLog;
      Global.Client.Ready += ClientReady;

      if (!await ClientStart())
      {
        return;
      }

      await Handler.Initialize();
    }

    protected override void OnStop()
    {
      _ = Logger.Instance.Write(new LogMessage("Service stopped", "HumanService:OnStop"));
      ClientStop().GetAwaiter().GetResult();
    }

    protected override void OnPause()
    {
      _ = Logger.Instance.Write(new LogMessage("Service paused", "HumanService:OnPause"));
      ClientStop().GetAwaiter().GetResult();
    }

    protected override void OnContinue()
    {
      _ = Logger.Instance.Write(new LogMessage("Service continued", "HumanService:OnContinue"));
      ClientStart().GetAwaiter().GetResult();
    }

    #region Discord client methods

    private async Task<bool> ClientStart()
    {
      try
      {
        var cfg = new Config();
        await Global.Client.LoginAsync(TokenType.Bot, cfg.Bot.Token);
        await Global.Client.StartAsync();
      }
      catch (Discord.Net.HttpException e)
      {
        _ = Logger.Instance.Write(new LogException(e, "HumanService:ClientStart"));
        return false;
      }
      return true;
    }

    private async Task<bool> ClientStop()
    {
      try
      {
        await Global.Client.LogoutAsync();
        await Global.Client.StopAsync();
      }
      catch (Discord.Net.HttpException e)
      {
        _ = Logger.Instance.Write(new LogException(e, "HumanService:ClientStart"));
        return false;
      }
      return true;
    }

    private async Task ClientReady()
    {
      var c = new Config();
      try
      {
        var newGuilds = Global.Client.Guilds
          .Select(x => x.Id)
          .Where(gid => !c.Has(gid));

        foreach (var gid in newGuilds)
        {
          c.Push(gid);
        }
      }
      catch (Exception e)
      {
        await Logger.Instance.Write(new LogException(e, "HumanService:ClientReady"));
      }
      finally
      {
        c.Save();
      }
    }

    private async Task ClientLog(Discord.LogMessage arg)
    {
      if (arg.Exception != null)
      {
        await Logger.Instance.Write(new LogException(arg.Exception, arg.Source, arg.Severity));
      }
      else
      {
        await Logger.Instance.Write(new LogMessage(arg.Message, arg.Source, arg.Severity));
      }
    }

    #endregion
  }
}
