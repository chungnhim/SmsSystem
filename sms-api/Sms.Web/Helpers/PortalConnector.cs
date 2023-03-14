using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Sms.Web.Models;

public class PortalConnector : IDisposable
{
  private readonly HttpClient _httpClient;
  public PortalConnector(PortalConnection connection)
  {
    _httpClient = new HttpClient();
    _httpClient.BaseAddress = new Uri(connection.PortalEndPoint);
    _httpClient.DefaultRequestHeaders.Accept.Clear();
    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Custom", PortalPayloadHelpers.GenerateKey(connection.PortalKey));
  }
  private string BuildUrl(string uri, Dictionary<string, string> queryString)
  {
    var query = HttpUtility.ParseQueryString(string.Empty);
    foreach (var item in queryString)
    {
      query[item.Key] = item.Value;
    }
    return $"{uri}?{query.ToString()}";
  }
  public async Task<T> GetFromPortal<T>(string url, Dictionary<string, string> query)
  {
    HttpResponseMessage response = await _httpClient.GetAsync(BuildUrl(url, query));
    if (response.IsSuccessStatusCode)
    {
      return await response.Content.ReadAsAsync<T>();
    }
    throw new Exception(string.Format("Cannot connect to portal {0}", _httpClient.BaseAddress));
  }

  public async Task<T> PostToPortal<T, K>(string url, K obj)
  {
    HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, obj);
    if (response.IsSuccessStatusCode)
    {
      return await response.Content.ReadAsAsync<T>();
    }
    throw new Exception(string.Format("Cannot connect to portal {0}", _httpClient.BaseAddress));
  }

  public void Dispose()
  {
    _httpClient.Dispose();
  }
}