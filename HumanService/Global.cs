using Discord.WebSocket;
using System;
using System.Globalization;
using System.IO;

namespace HumanService
{
  public static class Global
  {
    internal static string Resources
    {
      get
      {
        var path = AppDomain.CurrentDomain.BaseDirectory + "\\Resources";
        if (!Directory.Exists(path))
        {
          Directory.CreateDirectory(path);
        }
        return path;
      }
    }

    internal static DiscordSocketClient Client { get; set; }

    internal static string FormatTime() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
  }
}
