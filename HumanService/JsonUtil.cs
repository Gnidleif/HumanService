using Newtonsoft.Json;
using System.IO;

namespace HumanService
{
  public static class JsonUtil
  {
    public static bool TryWrite<T>(string path, T data)
    {
      var json = JsonConvert.SerializeObject(data, Formatting.None);
      File.WriteAllText(path, json);
      return json.Length > 0;
    }

    public static bool TryRead<T>(string path, out T data)
    {
      data = default(T);
      if (File.Exists(path))
      {
        var json = File.ReadAllText(path);
        data = JsonConvert.DeserializeObject<T>(json);
        return true;
      }
      return false;
    }
  }
}
