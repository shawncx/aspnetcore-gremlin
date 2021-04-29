using System.Collections.Generic;
using System.Threading.Tasks;
using aspnetcore_gremlin.Models;

namespace aspnetcore_gremlin.Services
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<MyItem>> GetItemsAsync();
        //Task<MyItem> GetItemAsync(string id);
        //Task AddItemAsync(MyItem item);
        //Task UpdateItemAsync(string id, MyItem item);
        //Task DeleteItemAsync(string id);
    }
}
