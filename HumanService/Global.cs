using Discord;
using Discord.WebSocket;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace HumanService
{
  public static class Global
  {
    private static string path;
    internal static string Resources
    {
      get
      {
        if (string.IsNullOrEmpty(path))
        {
          path = AppDomain.CurrentDomain.BaseDirectory + "\\Resources";
          if (!Directory.Exists(path))
          {
            Directory.CreateDirectory(path);
          }
        }
        return path;
      }
    }

    internal static DiscordSocketClient Client { get; set; }

    internal static string FormatTime() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    internal static long ToUnixTime() => new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();

    internal static async Task WriteOwner(string message)
    {
      try
      {
        var cfg = new Config();
        var owner = Global.Client.GetUser(cfg.Bot.Owner);
        await UserExtensions.SendMessageAsync(owner, message);
      }
      catch (NullReferenceException e)
      {
        _ = Logger.Instance.WriteAsync(new LogException(e, "Global:WriteOwner", LogSeverity.Error));
      }
    }
  }
}
