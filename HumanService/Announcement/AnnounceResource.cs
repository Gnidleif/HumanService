using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HumanService.Announcement
{
  public class AnnounceResource : IResource
  {
    private static readonly Lazy<AnnounceResource> lazy = new Lazy<AnnounceResource>(() => new AnnounceResource());
    private string Path { get; } = Global.Resources + "\\announce.json";
    private Dictionary<ulong, KeyValuePair<ulong, Info>> List { get; set; }

    public static AnnounceResource Instance { get { return lazy.Value; } }

    private AnnounceResource()
    {
      Global.Client.UserJoined += Client_UserJoined;
      Global.Client.UserLeft += Client_UserLeft;
    }

    public void Initialize()
    {
      var temp = new Dictionary<ulong, KeyValuePair<ulong, Info>>();
      if (File.Exists(Path) ? JsonUtil.TryRead(Path, out temp) : JsonUtil.TryWrite(Path, temp))
      {
        List = temp;
      }
    }

    public bool SetChannel(ulong gid, ulong cid)
    {
      if (!List.ContainsKey(gid))
      {
        return false;
      }
      var val = List[gid].Value;
      List.Remove(gid);
      List.Add(gid, new KeyValuePair<ulong, Info>(cid, val));
      return true;
    }

    public bool SetState(ulong gid, string key, bool state)
    {
      if (!List.ContainsKey(gid) || List[gid].Value.Events.ContainsKey(key))
      {
        return false;
      }
      var msg = List[gid].Value.Events[key.ToLower()].Item1;
      List[gid].Value.Events[key] = new Tuple<string, bool>(msg, state);
      return true;
    }

    public bool SetMessage(ulong gid, string key, string msg)
    {
      if (!List.ContainsKey(gid) || !List[gid].Value.Events.ContainsKey(key))
      {
        return false;
      }
      var state = List[gid].Value.Events[key.ToLower()].Item2;
      List[gid].Value.Events[key] = new Tuple<string, bool>(msg, state);
      return true;
    }

    public Dictionary<string, string> GetAnnouncements(ulong gid)
    {
      if (!List.ContainsKey(gid) || List[gid].Value.Events.Count == 0)
      {
        return null;
      }
      var result = new Dictionary<string, string>
      {
        { List[gid].Key.ToString(), null },
      };
      foreach (var a in List[gid].Value.Events)
      {
        result.Add(a.Key, $"**Message**: '{a.Value.Item1}'\n**State**: " + (a.Value.Item2 == true ? "Enabled" : "Disabled"));
      }
      return result;
    }

    public bool Has(ulong gid, ulong cid) => List.ContainsKey(gid) && List[gid].Key == cid;

    public bool Push(ulong gid, ulong cid)
    {
      if (!List.ContainsKey(gid))
      {
        List.Add(gid, new KeyValuePair<ulong, Info>());
      }
      if (List[gid].Key == 0)
      {
        List[gid] = new KeyValuePair<ulong, Info>(cid, new Info());
        return true;
      }
      return false;
    }

    public async Task Save() => await Task.Run(() => { JsonUtil.TryWrite(Path, List); });

    private async Task Client_UserJoined(SocketGuildUser arg)
    {
      var gid = arg.Guild.Id;
      if (List.ContainsKey(gid))
      {
        var ev = List[gid].Value.Events["userjoined"];
        var ch = Global.Client.GetChannel(List[gid].Key) as SocketTextChannel;
        if (ch != null && ev.Item2 == true)
        {
          await ch.SendMessageAsync(string.Format(ev.Item1, arg.Mention));
        }
      }
      _ = Logger.Instance.Write(new LogCommand(arg, arg.Guild, "User joined", "AnnounceResource:Client_UserJoined"));
    }

    private async Task Client_UserLeft(SocketGuildUser arg)
    {
      var gid = arg.Guild.Id;
      if (List.ContainsKey(gid))
      {
        var ev = List[gid].Value.Events["userleft"];
        var ch = Global.Client.GetChannel(List[gid].Key) as SocketTextChannel;
        if (ch != null && ev.Item2 == true)
        {
          await ch.SendMessageAsync(string.Format(ev.Item1, arg.Username));
        }
      }
      _ = Logger.Instance.Write(new LogCommand(arg, arg.Guild, "User left", "AnnounceResource:Client_UserLeft"));
    }

    private class Info
    {
      public Dictionary<string, Tuple<string, bool>> Events { get; set; } = new Dictionary<string, Tuple<string, bool>>
      {
        { "userjoined", new Tuple<string, bool>(":white_check_mark: {0} just joined the server, welcome!", true) },
        { "userleft", new Tuple<string, bool>(":negative_squared_cross_mark: {0} just left the server, good bye", true) },
      };
    }
  }
}
