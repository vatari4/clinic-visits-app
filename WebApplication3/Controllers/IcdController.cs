using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{
    public class IcdController : Controller
    {
        private readonly AppDbContext _db;
        public IcdController(AppDbContext db) => _db = db;

        // Страница со справочником
        public IActionResult Index() => View();

        // Возвращает список кодов МКБ-10 в JSON
        [HttpGet]
        public async Task<IActionResult> List(string? query)
        {
            var q = _db.IcdCodes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
                q = q.Where(c => c.Code.Contains(query) || c.Name.Contains(query));

            var items = await q
                .OrderBy(c => c.Code)
                .Select(c => new { code = c.Code, name = c.Name })
                .ToListAsync();

            return Json(items);
        }
    }
}
