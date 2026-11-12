using System.ComponentModel.DataAnnotations;
using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>The "New project" form: used as the form model and as the service input.</summary>
    public class CreateProjectDto
    {
        [Required(ErrorMessage = ValidationMessages.Required)]
        [StringLength(200)]
        [Display(Name = "Project name")]
        public string Name { get; set; } = string.Empty;

        [MinLength(1, ErrorMessage = ValidationMessages.Required)]
        [Display(Name = "PCI Pal services")]
        public List<PciPalService> Services { get; set; } = new();
    }
}
