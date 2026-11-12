namespace AOB.BAL.Common.Models
{
    /// <summary>
    /// Subject and HTML body of an email. Placeholders look like {{Name}}.
    /// </summary>
    public record EmailTemplate(string Subject, string Body);
}
