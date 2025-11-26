using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    /// Справочник МКБ-10 — минимальный набор полей: код, название, родитель.
    public class IcdCode
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Код записи из источника (не GUID), может быть пустым
        [StringLength(50)]
        public string? RecCode { get; set; }

        // Код МКБ-10 (например "H53.2")
        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        // Наименование по МКБ-10
        [Required]
        [StringLength(500)]
        public string Name { get; set; } = string.Empty;

        // Родительский элемент (для иерархии) — GUID внутрь нашей БД
        public Guid? ParentId { get; set; }

        // Актуальность (1 — актуально), можно хранить как bool
        public bool IsActual { get; set; } = true;
    }
}
