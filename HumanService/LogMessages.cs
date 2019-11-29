using Discord;
using System;
using System.Globalization;

namespace HumanService
{
  public abstract class IMessage
  {
    public string Source { get; }
    public string Date { get; }
    public string Time { get; }
    public string Level { get; }
    public LogSeverity Code { get; }

    public IMessage(string src, LogSeverity level)
    {
      Source = src;
      Date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
      Time = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
      Level = Enum.GetName(typeof(LogSeverity), level);
      Code = level;
    }
  }

  public class LogMessage : IMessage
  {
    public string Text { get; }

    public LogMessage(string text, string source, LogSeverity level = LogSeverity.Info) : base(source, level)
    {
      Text = text;
    }
  }

  public class LogException : IMessage
  {
    public string Exception { get; }

    public LogException(Exception e, string source, LogSeverity level = LogSeverity.Critical) : base(source, level)
    {
      Exception = e.ToString();
    }
  }

  public class LogCommand : IMessage
  {
    public string User { get; set; }
    public ulong UserId { get; set; }
    public string Guild { get; set; }
    public ulong GuildId { get; set; }
    public string Info { get; set; }

    public LogCommand(IUser user, IGuild guild, string info, string source, LogSeverity level = LogSeverity.Info) : base(source, level)
    {
      User = user.Username;
      UserId = user.Id;
      Guild = guild.Name;
      GuildId = guild.Id;
      Info = info;
    }
  }
}
