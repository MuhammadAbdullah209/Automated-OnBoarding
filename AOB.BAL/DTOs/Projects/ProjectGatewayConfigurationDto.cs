using System.ComponentModel.DataAnnotations;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Projects
{
    /// <summary>
    /// "Adaptive Configuration" screen (form model and service input). Authentication and required
    /// gateway fields are added automatically; only the selectable ones the customer ticks (Conditional
    /// or Optional) are listed here, as ids of the gateway fields.
    /// </summary>
    public class ProjectGatewayConfigurationDto
    {
        /// <summary>Selectable input fields the customer ticked (PaymentGatewayField ids).</summary>
        public List<Guid> SelectedInputFields { get; set; } = new();

        /// <summary>Selectable output fields the customer ticked (PaymentGatewayField ids).</summary>
        public List<Guid> SelectedOutputFields { get; set; } = new();

        public List<PostTransactionMethod> PostTransactionMethods { get; set; } = new();

        // Required when PostTransactionMethods contains Redirect: where customers are sent after paying.
        [StringLength(500), Display(Name = "Redirect URL")]
        public string? RedirectUrl { get; set; }

        // Contact centre details, only asked (and required) when the customer takes phone payments with an agent.
        [Display(Name = "Contact centre platform")]
        public ContactCentrePlatform? ContactCentrePlatform { get; set; }

        [Display(Name = "Connection type")]
        public ContactCentreConnectionType? ConnectionType { get; set; }

        [StringLength(2000), Display(Name = "Call and payment handover notes")]
        public string? HandoverNotes { get; set; }
    }
}
