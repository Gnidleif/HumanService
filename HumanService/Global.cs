using Discord.WebSocket;
using System;
using System.IO;

namespace HumanService
{
  static class Global
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
  }
}
