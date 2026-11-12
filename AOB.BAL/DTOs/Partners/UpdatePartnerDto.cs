namespace AOB.BAL.DTOs.Partners
{
    /// <summary>Editable partner details: organisation name and login email.</summary>
    public class UpdatePartnerDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
