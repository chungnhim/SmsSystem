using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
  public interface IPreloadOrderSimCountryQueue
  {
    void QueuePreloadSimCountry(int simCountryId);
    int? DequeuePreloadSimCountry();

    IEnumerable<int> DequeueAllPreloadSimCountry();
  }
  public class PreloadOrderSimCountryQueue : IPreloadOrderSimCountryQueue
  {
    private readonly ConcurrentQueue<int> SimCountryIdQueue = new ConcurrentQueue<int>();

    public IEnumerable<int> DequeueAllPreloadSimCountry()
    {
      int? id = null;
      do
      {
        id = DequeuePreloadSimCountry();
        if (id.HasValue)
        {
          yield return id.Value;
        }
      } while (id != null);
    }

    public int? DequeuePreloadSimCountry()
    {
      if (SimCountryIdQueue.TryDequeue(out int result))
      {
        return result;
      }
      return null;
    }

    public void QueuePreloadSimCountry(int simCountryId)
    {
      if (SimCountryIdQueue.Contains(simCountryId))
      {
        return;
      }
      SimCountryIdQueue.Enqueue(simCountryId);
    }

  }
}
