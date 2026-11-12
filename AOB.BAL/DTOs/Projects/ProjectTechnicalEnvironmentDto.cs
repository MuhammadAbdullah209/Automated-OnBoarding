using System.ComponentModel.DataAnnotations;
using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>"Technical Environment" screen (form model and service input).</summary>
    public class ProjectTechnicalEnvironmentDto
    {
        [Display(Name = "Business system / CRM")]
        public CrmPlatform? CrmPlatform { get; set; }

        [Display(Name = "Regions where payments are processed")]
        public List<PaymentRegion> PaymentRegions { get; set; } = new();

        [MinLength(1, ErrorMessage = ValidationMessages.Required)]
        [Display(Name = "Staging regions")]
        public List<PciPalRegion> StagingRegions { get; set; } = new();

        [MinLength(1, ErrorMessage = ValidationMessages.Required)]
        [Display(Name = "Production regions")]
        public List<PciPalRegion> ProductionRegions { get; set; } = new();

        [Required(ErrorMessage = ValidationMessages.Required)]
        [Display(Name = "Network access readiness")]
        public OutboundAccess? OutboundAccess { get; set; }
    }
}
