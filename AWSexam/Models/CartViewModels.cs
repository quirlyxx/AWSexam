using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AWSexam.Models
{
    public record CartLineVm(Product Product, int Quantity)
    {
        public decimal Subtotal => Product.Price * Quantity;
    }

    public class CartVm
    {
        public List<CartLineVm> Lines { get; set; } = new();
        public decimal Total => Lines.Sum(l => l.Subtotal);
    }

    public class CheckoutVm
    {
        [Required(ErrorMessage = "Вкажіть ім'я"), StringLength(120)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть email"), EmailAddress(ErrorMessage = "Некоректний email"), StringLength(120)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть телефон"), StringLength(30)]
        public string Phone { get; set; } = string.Empty;
        [BindNever, ValidateNever]
        public CartVm Cart { get; set; } = new();
    }
}