namespace AOB.BAL.Common.Enums
{
    /// <summary>
    /// Where a document request stands overall, for the lists, dashboards and the project approval gate. It follows
    /// its items (see ProjectInformationRequestItem.Status): waiting for the customer, waiting for review, or closed.
    /// </summary>
    public enum InformationRequestStatus
    {
        /// <summary>The customer still has something to provide (a pending or rejected item).</summary>
        Requested = 1,

        /// <summary>The customer has responded; an internal user needs to review it.</summary>
        Submitted = 2,

        /// <summary>Closed: every item was approved or resolved.</summary>
        Approved = 3
    }

    /// <summary>The lifecycle of one requested document. Each request moves through it on its own.</summary>
    public enum InformationItemStatus
    {
        /// <summary>Raised; the customer has not responded yet.</summary>
        Pending = 1,

        /// <summary>The customer responded (a document, a note, or both); waiting for review.</summary>
        Submitted = 2,

        /// <summary>The internal user approved the submitted document.</summary>
        Approved = 3,

        /// <summary>The internal user rejected the response, with a reason; the customer has to respond again.</summary>
        Rejected = 4,

        /// <summary>The customer responded again after a rejection; waiting for review.</summary>
        Resubmitted = 5,

        /// <summary>The internal user closed the request without approving a document.</summary>
        Resolved = 6
    }

    /// <summary>What an internal user decides about a customer's response.</summary>
    public enum InformationReviewDecision
    {
        /// <summary>Accept the attached document. Only possible when the response has a document.</summary>
        Approve = 1,

        /// <summary>Send it back to the customer with a reason; they respond again on the same request.</summary>
        Reject = 2,

        /// <summary>Close the request without approving a document.</summary>
        Resolve = 3
    }
}
