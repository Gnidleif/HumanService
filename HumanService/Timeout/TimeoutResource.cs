using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace HumanService.Timeout
{
  public class TimeoutResource : IResource
  {
    private static Lazy<TimeoutResource> lazy { get; } = new Lazy<TimeoutResource>(() => new TimeoutResource());
    private string Path { get; } = Global.Resources + "\\timeout.json";
    private Dictionary<ulong, Dictionary<ulong, Info>> List { get; set; }

    public static TimeoutResource Instance { get { return lazy.Value; } }

    private TimeoutResource()
    {
      var temp = new Dictionary<ulong, Dictionary<ulong, Info>>();
      if (File.Exists(Path) ? JsonUtil.TryRead(Path, out temp) : JsonUtil.TryWrite(Path, temp))
      {
        List = new Dictionary<ulong, Dictionary<ulong, Info>>();
        foreach (var gid in temp.Keys)
        {
          foreach (var uid in temp[gid].Keys)
          {
            if (!Push(gid, uid))
            {
              continue;
            }
            List[gid][uid] = new Info();
            List[gid][uid].RoleIds.AddRange(temp[gid][uid].RoleIds);
            var tick = MakeTimer(gid, uid, temp[gid][uid].Time);
            if (tick == null)
            {
              UnknownUnset(gid, uid).GetAwaiter().GetResult();
              _ = Pop(gid, uid);
            }
            else
            {
              List[gid][uid].Time = temp[gid][uid].Time;
              List[gid][uid].Tick = tick;
            }
          }
        }
      }
    }

    public async Task Save()
    {
      var toDelete = List.Keys.Where(x => !List[x].Any()).ToList();
      toDelete.ForEach(x => List.Remove(x));

      await Task.Run(() => { JsonUtil.TryWrite(Path, List); });
    }

    public bool Has(ulong gid, ulong uid) => List.ContainsKey(gid) && List[gid].ContainsKey(uid);

    public bool Pop(ulong gid) => List.Remove(gid);

    public bool Pop(ulong gid, ulong uid) => Has(gid, uid) ? List[gid].Remove(uid) : false;

    public bool Push(ulong gid, ulong uid)
    {
      if (!List.ContainsKey(gid))
      {
        List.Add(gid, new Dictionary<ulong, Info>());
      }
      if (!List[gid].ContainsKey(uid))
      {
        List[gid].Add(uid, null);
        return true;
      }
      return false;
    }

    public async Task SetTimeout(IGuildUser user, uint minutes, List<ulong> roles = null)
    {
      var time = DateTime.Now.AddMinutes(minutes);
      if (roles == null)
      {
        roles = user.RoleIds.ToList();
      }
      var gid = user.GuildId;
      var uid = user.Id;
      if (!Push(gid, uid))
      {
        roles.AddRange(List[gid][uid].RoleIds);
        roles = roles.Distinct().ToList();
      }
      List[gid][uid] = new Info
      {
        RoleIds = roles,
        Time = time,
        Tick = MakeTimer(gid, uid, time),
      };
      if (List[gid][uid].Tick == null)
      {
        try
        {
          _ = UnsetTimeout(user);
        }
        catch (Exception e)
        {
          _ = Logger.Instance.Write(new LogException(e, "TimeoutResource:SetTimeout", LogSeverity.Error));
        }
      }
      else
      {
        try
        {
          var roleIds = roles
            .Select(x => user.Guild.GetRole(x))
            .Where(x => !x.IsManaged && x != user.Guild.EveryoneRole)
            .ToList();
          await user.RemoveRolesAsync(roleIds);
        }
        catch (Exception e)
        {
          _ = Logger.Instance.Write(new LogException(e, "TimeoutResource:SetTimeout", LogSeverity.Error));
        }
      }
    }

    public async Task UnsetTimeout(IGuildUser user)
    {
      if (!Has(user.GuildId, user.Id))
      {
        return;
      }
      try
      {
        var roles = List[user.GuildId][user.Id].RoleIds
          .Select(x => user.Guild.GetRole(x))
          .Where(x => !x.IsManaged && x != user.Guild.EveryoneRole)
          .ToList();
        _ = user.AddRolesAsync(roles);
        _ = Logger.Instance.Write(new LogCommand(user, user.Guild, "Timeout expired", "TimeoutResource:UnsetTimeout"));
      }
      catch (Exception e)
      {
        _ = Logger.Instance.Write(new LogException(e, "TimeoutResource:UnsetTimeout", LogSeverity.Error));
        return;
      }
      await Save();
    }

    private async Task UnknownUnset(ulong gid, ulong uid)
    {
      var g = Global.Client.GetGuild(gid);
      var u = g?.GetUser(uid);
      if (g == null)
      {
        Pop(gid);
      }
      else if (u == null)
      {
        Pop(gid, uid);
      }
      else
      {
        await UnsetTimeout(u);
      }
    }

    private Timer MakeTimer(ulong gid, ulong uid, DateTime time)
    {
      var diff = (time - DateTime.Now).TotalMilliseconds;
      if (diff < 0)
      {
        return null;
      }
      var tick = new Timer
      {
        Interval = diff,
        AutoReset = false,
        Enabled = true,
      };
      tick.Elapsed += (object sender, ElapsedEventArgs e) =>
      {
        _ = UnknownUnset(gid, uid);
      };
      return tick;
    }

    private class Info
    {
      public List<ulong> RoleIds { get; set; } = new List<ulong>();
      public DateTime Time { get; set; }
      [JsonIgnore]
      public Timer Tick { get; set; }
    }
  }
}
