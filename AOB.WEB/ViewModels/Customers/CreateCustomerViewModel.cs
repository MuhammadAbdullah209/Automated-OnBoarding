using System.ComponentModel.DataAnnotations;

namespace AOB.WEB.ViewModels.Customers
{
    /// <summary>Form a partner uses to add a customer; only the company name is captured.</summary>
    public class CreateCustomerViewModel
    {
        [Required, StringLength(200), Display(Name = "Company name")]
        public string CompanyName { get; set; } = string.Empty;
    }
}
