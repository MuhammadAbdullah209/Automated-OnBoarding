using AOB.BAL.Common.Enums;

namespace AOB.WEB.Helpers
{
    /// <summary>The wording and badge colours for document requests, in one place for the staff and customer pages.</summary>
    public static class InformationRequestUi
    {
        public static string ItemBadge(InformationItemStatus s) => s switch
        {
            InformationItemStatus.Approved => "badge-green",
            InformationItemStatus.Resolved => "badge-green",
            InformationItemStatus.Submitted => "badge-navy",
            InformationItemStatus.Resubmitted => "badge-navy",
            InformationItemStatus.Rejected => "badge-red",
            _ => "badge-gray"
        };

        public static string ItemLabel(InformationItemStatus s) => s switch
        {
            InformationItemStatus.Approved => "Approved",
            InformationItemStatus.Resolved => "Resolved",
            InformationItemStatus.Submitted => "Submitted",
            InformationItemStatus.Resubmitted => "Resubmitted",
            InformationItemStatus.Rejected => "Rejected",
            _ => "Pending"
        };

        /// <summary>The status as the customer reads it: what happens next.</summary>
        public static string CustomerItemLabel(InformationItemStatus s) => s switch
        {
            InformationItemStatus.Submitted => "Submitted · in review",
            InformationItemStatus.Resubmitted => "Resubmitted · in review",
            InformationItemStatus.Rejected => "Rejected · action needed",
            InformationItemStatus.Pending => "Pending · action needed",
            _ => ItemLabel(s)
        };
    }
}
