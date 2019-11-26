using Discord.Commands;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HumanService.TypeReaders
{
  public class RegexTypeReader : TypeReader
  {
    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
    {
      try
      {
        var result = new Regex(input);
        if (result != null)
        {
          return Task.FromResult(TypeReaderResult.FromSuccess(result));
        }
      }
      catch (Exception e)
      {
        _ = Logger.Instance.Write(new LogException(e, "RegexTypeReader:ReadAsync", Discord.LogSeverity.Error));
      }
      return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Failed to parse Regex."));
    }
  }
}
