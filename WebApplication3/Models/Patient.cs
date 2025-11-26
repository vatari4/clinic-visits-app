using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApplication3.Models
{
    public class Patient
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50, ErrorMessage = "Фамилия не может превышать 50 символов")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(50, ErrorMessage = "Имя не может превышать 50 символов")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Отчество не может превышать 50 символов")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Дата рождения обязательна")]
        public DateTime BirthDate { get; set; }

        [StringLength(15, ErrorMessage = "Телефон не может превышать 15 символов")]
        public string? Phone { get; set; }

        public List<Visit> Visits { get; set; } = new List<Visit>();
    }
}
