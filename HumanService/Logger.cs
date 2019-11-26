using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HumanService
{
  public class Logger
  {
    #region Attributes
    private static Lazy<Logger> lazy { get; } = new Lazy<Logger>(() => new Logger());

    public static Logger Instance { get { return lazy.Value; } }
    public Discord.LogSeverity Level { get; set; } = Discord.LogSeverity.Info;
    public string Path { get; } = Global.Resources + "\\Logs";

    #endregion

    #region Constructors

    private Logger()
    {
      if (!Directory.Exists(Path))
      {
        Directory.CreateDirectory(Path);
      }
    }

    #endregion

    #region Public methods

    public async Task Write(IMessage data)
    {
      if (data.Code <= Level)
      {
        var path = GetFilePath();
        var json = JsonConvert.SerializeObject(data, Formatting.None) + Environment.NewLine;
        var text = Encoding.Unicode.GetBytes(json);
        using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
        {
          await stream.WriteAsync(text, 0, text.Length);
        }
      }
    }

    public async Task<string> Read(DateTime? date = null)
    {
      var path = GetFilePath(date);
      if (!File.Exists(path))
      {
        return string.Empty;
      }
      using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
      {
        var sb = new StringBuilder();
        var buffer = new byte[0x1000];
        var numRead = default(int);
        while ((numRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
          sb.Append(Encoding.Unicode.GetString(buffer, 0, numRead));
        }

        return sb.ToString();
      }
    }

    #endregion

    #region Private methods

    private string GetFilePath(DateTime? date = null) => string.Format("{0}\\{1}.json", Path, (date ?? DateTime.Now).Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

    #endregion
  }
}
