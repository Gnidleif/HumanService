using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HumanService
{
  public class Config : IResource
  {
    private string Path = Global.Resources + "\\config.json";

    public BotConfig Bot { get; set; }

    public Config()
    {
      var temp = new BotConfig();
      if (File.Exists(this.Path) ? JsonUtil.TryRead(this.Path, out temp) : JsonUtil.TryWrite(this.Path, temp))
      {
        Bot = temp;
        _ = Save();
      }
    }

    public bool Has(ulong gid) => Bot.Guilds.ContainsKey(gid);

    public void Push(ulong gid) => Bot.Guilds.Add(gid, new GuildConfig());

    public bool Pop(ulong gid) => Bot.Guilds.Remove(gid);

    public async Task Save() => await Task.Run(() => { JsonUtil.TryWrite(this.Path, Bot); });

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
      public WelcomeConfig Welcome { get; set; } = new WelcomeConfig();
    }

    public class WelcomeConfig
    {
      public bool Enabled { get; set; } = false;
      public uint Time { get; set; } = 10;
      public ulong BaseRole { get; set; } = 0;
      public string Message { get; set; } = "Welcome! You'll gain full privileges soon.";
    }
  }
}
