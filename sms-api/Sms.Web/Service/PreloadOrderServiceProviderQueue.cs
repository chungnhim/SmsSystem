using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
  public interface IPreloadOrderServiceProviderQueue
  {
    void QueuePreloadService(int serviceProviderId);
    int? DequeuePreloadService();

    IEnumerable<int> DequeueAllPreloadService();
  }
  public class PreloadOrderServiceProviderQueue : IPreloadOrderServiceProviderQueue
  {
    private readonly ConcurrentQueue<int> ServiceProviderIdQueue = new ConcurrentQueue<int>();

    public IEnumerable<int> DequeueAllPreloadService()
    {
      int? id = null;
      do
      {
        id = DequeuePreloadService();
        if (id.HasValue)
        {
          yield return id.Value;
        }
      } while (id != null);
    }

    public int? DequeuePreloadService()
    {
      if (ServiceProviderIdQueue.TryDequeue(out int result))
      {
        return result;
      }
      return null;
    }

    public void QueuePreloadService(int serviceProviderId)
    {
      if (ServiceProviderIdQueue.Contains(serviceProviderId))
      {
        return;
      }
      ServiceProviderIdQueue.Enqueue(serviceProviderId);
    }

  }
}
