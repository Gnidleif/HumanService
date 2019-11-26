using System.IO;
using System.ServiceProcess;

namespace HumanService
{
  class Program
  {
    private static void Main()
    {
      if (!Directory.Exists(Global.Resources))
      {
        Directory.CreateDirectory(Global.Resources);
      }

      var ServicesToRun = new ServiceBase[]
      {
        new HumanService()
      };

      ServiceBase.Run(ServicesToRun);
    }
  }
}
