using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    public class VisitsController : Controller
    {
        private readonly AppDbContext _db;
        public VisitsController(AppDbContext db) => _db = db;

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> List(Guid patientId)
        {
            var visits = await _db.Visits
                .Where(v => v.PatientId == patientId)
                .OrderByDescending(v => v.VisitDate)
                .Select(v => new
                {
                    id = v.Id,
                    visitDate = v.VisitDate.ToString("yyyy-MM-dd"),
                    icdCode = v.IcdCode != null ? v.IcdCode.Code : v.IcdCodeText,
                    description = v.Description
                })
                .ToListAsync();

            return Json(visits);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid patientId, DateTime visitDate, string? icdCodeText, Guid? icdCodeId, string? description)
        {
            var patient = await _db.Patients.FindAsync(patientId);
            if (patient == null)
                return NotFound(new { error = "Пациент не найден" });

            if (visitDate == default)
            {
                ModelState.AddModelError(nameof(visitDate), "Дата визита обязательна");
                return BadRequest(ModelState);
            }

            var visit = new Visit
            {
                PatientId = patientId,
                VisitDate = DateTime.SpecifyKind(visitDate, DateTimeKind.Utc),
                IcdCodeId = icdCodeId,
                IcdCodeText = icdCodeText,
                Description = description
            };

            _db.Visits.Add(visit);
            await _db.SaveChangesAsync();

            return Ok(new { id = visit.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var visit = await _db.Visits
                .Include(v => v.Patient)
                .Include(v => v.IcdCode)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (visit == null) return NotFound();

            return Json(new
            {
                id = visit.Id,
                patient = $"{visit.Patient.LastName} {visit.Patient.FirstName} {visit.Patient.MiddleName}",
                visitDate = visit.VisitDate.ToString("yyyy-MM-dd"),
                icdCode = visit.IcdCode?.Code ?? visit.IcdCodeText,
                icdName = visit.IcdCode?.Name,
                description = visit.Description
            });
        }

        [HttpGet]
        public async Task<IActionResult> PatientsList()
        {
            var patients = await _db.Patients
                .OrderBy(p => p.LastName)
                .Select(p => new { id = p.Id, name = $"{p.LastName} {p.FirstName} {p.MiddleName}" })
                .ToListAsync();

            return Json(patients);
        }

        [HttpGet]
        public async Task<IActionResult> SearchIcd(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            term = term.Trim();

            var codes = await _db.IcdCodes
                .Where(c => c.Code.StartsWith(term) || c.Name.Contains(term))
                .OrderBy(c => c.Code)
                .Select(c => new
                {
                    id = c.Id,                           // GUID для связи с Visit.IcdCodeId
                    label = $"{c.Code} - {c.Name}",      // текст для автокомплита
                    value = c.Code                        // что вставляем в input
                })
                .Take(10)
                .ToListAsync();

            return Json(codes);
        }

    }
}
