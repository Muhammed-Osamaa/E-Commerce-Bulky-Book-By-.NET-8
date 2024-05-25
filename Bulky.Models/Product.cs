using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }

        [Required]
        public string ISBN { get; set; }
        [Required]
        public string Author { get; set; }

        [Required]
        [DisplayName("List Price")]
        [Range(1,100)]
        public double ListPrice { get; set; }

        [Required]
        [DisplayName("Price For 1-50")]
        [Range(1, 100)]
        public double Price { get; set; }

        [Required]
        [DisplayName("Price For 50")]
        [Range(1, 100)]
        public double Price50 { get; set; }

        [Required]
        [DisplayName("Price For 100+")]
        [Range(1, 100)]
        public double Price100 { get; set; }

        [ValidateNever]
        public string ImageUrl { get; set; }

        [DisplayName("Categroy Id")]
        public int Id { get; set; }
        [ForeignKey("Id")]
        [ValidateNever]
        public Category Category { get; set; }
    }
}
