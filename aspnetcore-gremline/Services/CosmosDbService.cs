using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using aspnetcore_gremline.Models;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace aspnetcore_gremline.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private GremlinClient _client;

        public CosmosDbService(string hostname, int port, string username, string password)
        {
            GremlinServer server = new GremlinServer(hostname, port, true, username, password);
            ConnectionPoolSettings connectionPoolSettings = new ConnectionPoolSettings()
            {
                MaxInProcessPerConnection = 10,
                PoolSize = 30,
                ReconnectionAttempts = 3,
                ReconnectionBaseDelay = TimeSpan.FromMilliseconds(500)
            };
            var webSocketConfiguration =
                new Action<ClientWebSocketOptions>(options =>
                {
                    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                });
            _client = new GremlinClient(
                server,
                new GraphSON2Reader(),
                new GraphSON2Writer(),
                GremlinClient.GraphSON2MimeType,
                connectionPoolSettings,
                webSocketConfiguration);
        }

        //public async Task AddItemAsync(MyItem item)
        //{
        //    await this._container.CreateItemAsync<MyItem>(item, new PartitionKey(item.Id));
        //}

        //public async Task DeleteItemAsync(string id)
        //{
        //    await this._container.DeleteItemAsync<MyItem>(id, new PartitionKey(id));
        //}

        //public async Task<MyItem> GetItemAsync(string id)
        //{
        //    try
        //    {
        //        ItemResponse<MyItem> response = await this._container.ReadItemAsync<MyItem>(id, new PartitionKey(id));
        //        return response.Resource;
        //    }
        //    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        //    {
        //        return null;
        //    }

        //}

        public async Task<IEnumerable<MyItem>> GetItemsAsync()
        {
            ResultSet<dynamic> resultSet = await _client.SubmitAsync<dynamic>("g.V()").ConfigureAwait(false);
            IList<MyItem> list = new List<MyItem>();
            foreach (var result in resultSet)
            {
                list.Add(new MyItem() 
                {
                    ItemId = GetValue(result, "itemId"),
                    Name = GetValue(result, "Name")
                }); 
            }
            return list;
        }

        //public async Task UpdateItemAsync(string id, MyItem item)
        //{
        //    await this._container.UpsertItemAsync<MyItem>(item, new PartitionKey(id));
        //}

        private string GetValue(dynamic result, string key)
        {
            IDictionary<string, object> properties = (IDictionary<string, object>)((IDictionary<string, object>)result)["properties"];
            IList<object> enumrable = (properties[key] as IEnumerable<object>).Cast<object>().ToList();
            return ((IDictionary<string, object>)enumrable[0])["value"] as string;
        }
    }
}
