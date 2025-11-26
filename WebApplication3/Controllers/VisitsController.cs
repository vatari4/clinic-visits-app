using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication3.Data;
using WebApplication3.Models;
using System.Collections.Generic;

namespace WebApplication3.Controllers
{
    public class VisitsController : Controller
    {
        private readonly AppDbContext _db;
        public VisitsController(AppDbContext db) => _db = db;

        public IActionResult Index() => View();

        // Список визитов конкретного пациента
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

        // Создание визита
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid patientId, DateTime visitDate, string? icdCodeText, Guid? icdCodeId, string? description)
        {
            var patient = await _db.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

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

        // Автодополнение ICD
        [HttpGet]
        public async Task<IActionResult> SearchIcd(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Json(new List<object>());

            var codes = await _db.IcdCodes
                .Where(c => c.Code.StartsWith(term) || c.Name.Contains(term))
                .OrderBy(c => c.Code)
                .Select(c => new
                {
                    id = c.Id,
                    label = $"{c.Code} - {c.Name}",
                    value = c.Code
                })
                .Take(10)
                .ToListAsync();

            return Json(codes);
        }

        // Список пациентов для селекта
        [HttpGet]
        public async Task<IActionResult> PatientsList()
        {
            var patients = await _db.Patients
                .OrderBy(p => p.LastName)
                .Select(p => new { id = p.Id, name = $"{p.LastName} {p.FirstName} {p.MiddleName}" })
                .ToListAsync();

            return Json(patients);
        }
    }
}
