using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    public class PatientsController : Controller
    {
        private readonly AppDbContext _db;
        public PatientsController(AppDbContext db) => _db = db;

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> List(string? query, string? sort, string? dir)
        {
            var q = _db.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                q = q.Where(p =>
                    p.LastName.Contains(query) ||
                    p.FirstName.Contains(query) ||
                    (p.MiddleName != null && p.MiddleName.Contains(query)) ||
                    (p.Phone != null && p.Phone.Contains(query))
                );
            }

            bool asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);

            q = sort switch
            {
                "LastName" => asc ? q.OrderBy(p => p.LastName) : q.OrderByDescending(p => p.LastName),
                "FirstName" => asc ? q.OrderBy(p => p.FirstName) : q.OrderByDescending(p => p.FirstName),
                "BirthDate" => asc ? q.OrderBy(p => p.BirthDate) : q.OrderByDescending(p => p.BirthDate),
                _ => q.OrderBy(p => p.LastName)
            };

            var items = await q
                .Select(p => new
                {
                    id = p.Id,
                    lastName = p.LastName,
                    firstName = p.FirstName,
                    middleName = p.MiddleName,
                    birthDate = p.BirthDate.ToString("yyyy-MM-dd"),
                    phone = p.Phone,
                    fullName = p.LastName + " " + p.FirstName + (string.IsNullOrWhiteSpace(p.MiddleName) ? "" : " " + p.MiddleName)
                })
                .ToListAsync();

            return Json(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Patient model)
        {
            model.Id = Guid.NewGuid();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Приведение к UTC
            model.BirthDate = DateTime.SpecifyKind(model.BirthDate, DateTimeKind.Utc);

            _db.Patients.Add(model);
            await _db.SaveChangesAsync();
            return Ok(new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] Patient model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _db.Patients.FindAsync(model.Id);
            if (existing == null)
                return NotFound();

            existing.LastName = model.LastName;
            existing.FirstName = model.FirstName;
            existing.MiddleName = model.MiddleName;
            existing.Phone = model.Phone;
            existing.BirthDate = DateTime.SpecifyKind(model.BirthDate, DateTimeKind.Utc);

            await _db.SaveChangesAsync();
            return Ok();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p == null) return NotFound();

            _db.Patients.Remove(p);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Visits(Guid id)
        {
            var visits = await _db.Visits
                .Where(v => v.PatientId == id)
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
        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            var patient = await _db.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            return Json(new
            {
                id = patient.Id,
                lastName = patient.LastName,
                firstName = patient.FirstName,
                middleName = patient.MiddleName,
                birthDate = patient.BirthDate.ToString("yyyy-MM-dd"),
                phone = patient.Phone
            });
        }

            [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVisit(Guid patientId, DateTime visitDate, string? icdCodeText, Guid? icdCodeId, string? description)
        {
            var p = await _db.Patients.FindAsync(patientId);
            if (p == null) return NotFound();

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
        public async Task<IActionResult> ExportXml(Guid id)
        {
            var patient = await _db.Patients
                .Include(p => p.Visits)
                .ThenInclude(v => v.IcdCode)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null) return NotFound();

            var x = new System.Xml.Linq.XDocument(
                new System.Xml.Linq.XElement("PatientExport",
                    new System.Xml.Linq.XElement("Patient",
                        new System.Xml.Linq.XElement("LastName", patient.LastName),
                        new System.Xml.Linq.XElement("FirstName", patient.FirstName),
                        new System.Xml.Linq.XElement("MiddleName", patient.MiddleName ?? ""),
                        new System.Xml.Linq.XElement("BirthDate", patient.BirthDate.ToString("yyyy-MM-dd")),
                        new System.Xml.Linq.XElement("Phone", patient.Phone ?? "")
                    ),
                    new System.Xml.Linq.XElement("Visits",
                        patient.Visits.Select(v =>
                            new System.Xml.Linq.XElement("Visit",
                                new System.Xml.Linq.XElement("VisitDate", v.VisitDate.ToString("yyyy-MM-dd")),
                                new System.Xml.Linq.XElement("IcdCode", v.IcdCode?.Code ?? v.IcdCodeText ?? ""),
                                new System.Xml.Linq.XElement("Description", v.Description ?? "")
                            )
                        )
                    )
                )
            );

            var xmlString = x.ToString();
            var fileName = $"patient_{patient.LastName}_{patient.FirstName}.xml";
            return File(System.Text.Encoding.UTF8.GetBytes(xmlString), "application/xml", fileName);
        }
    }
}
