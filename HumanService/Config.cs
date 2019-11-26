using System.Collections.Generic;
using System.IO;

namespace HumanService
{
  public class Config : IResource
  {
    private string Path = Global.Resources + "\\config.json";

    public BotConfig Bot { get; set; }

    public Config()
    {
      var temp = new BotConfig();
      if (File.Exists(Path) ? JsonUtil.TryRead(Path, out temp) : JsonUtil.TryWrite(Path, temp))
      {
        Bot = temp;
      }
    }

    public bool Has(ulong gid) => Bot.Guilds.ContainsKey(gid);

    public void Push(ulong gid) => Bot.Guilds.Add(gid, new GuildConfig());

    public bool Pop(ulong gid) => Bot.Guilds.Remove(gid);

    public bool Save() => JsonUtil.TryWrite(Path, Bot);

    public class BotConfig
    {
      public string Token { get; set; }
      public ulong Owner { get; set; }
      public Dictionary<ulong, GuildConfig> Guilds { get; set; } = new Dictionary<ulong, GuildConfig>();
    }

    public class GuildConfig
    {
      public string Prefix { get; set; } = "!";
      public char Mark { get; set; } = '⭐';
    }
  }
}
