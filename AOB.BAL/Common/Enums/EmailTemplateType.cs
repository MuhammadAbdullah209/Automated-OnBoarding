namespace AOB.BAL.Common.Enums
{
    /// <summary>
    /// Identifies an email template. Today templates live in a class; later the same
    /// type can be used as the lookup key when they move to the database.
    /// </summary>
    public enum EmailTemplateType
    {
        EmailVerification = 1,
        PasswordResetOtp = 2,

        /// <summary>Sent to the customer's primary contact when staff ask for more information on a project.</summary>
        InformationRequested = 3,

        /// <summary>Sent to the customer's primary contact when an internal user asks for the users CSV.</summary>
        UsersCsvRequested = 4,

        /// <summary>
        /// Sent to a new internal user once their email is verified; carries the temporary password.
        /// Its body is never written to the log.
        /// </summary>
        InternalUserCredentials = 5,

        /// <summary>Sent when the project reaches Completed (automatically or marked by staff).</summary>
        ProjectCompleted = 6
    }
}
