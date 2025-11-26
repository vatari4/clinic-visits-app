using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class Visit
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public DateTime VisitDate { get; set; }

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public Guid? IcdCodeId { get; set; }
        public IcdCode? IcdCode { get; set; }

        [StringLength(36)]
        public string? IcdCodeText { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }
    }
}
