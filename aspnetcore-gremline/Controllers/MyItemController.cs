using System.Threading.Tasks;
using aspnetcore_gremline.Services;
using Microsoft.AspNetCore.Mvc;

namespace aspnetcore_gremline.Controllers
{
    public class MyItemController : Controller
    {
        private readonly ICosmosDbService _cosmosDbService;
        public MyItemController(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        [ActionName("Index")]
        public async Task<IActionResult> Index()
        {
            return View(await _cosmosDbService.GetItemsAsync());
        }

        [ActionName("Create")]
        public IActionResult Create()
        {
            return View();
        }
    }
}
