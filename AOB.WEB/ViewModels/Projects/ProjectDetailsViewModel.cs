using AOB.BAL.DTOs.Customers;
using AOB.BAL.DTOs.Projects;

namespace AOB.WEB.ViewModels.Projects
{
    public class ProjectDetailsViewModel
    {
        public ProjectFullDetailsDto Details { get; set; } = new();
        public CustomerProfileDto? Profile { get; set; }
    }
}
