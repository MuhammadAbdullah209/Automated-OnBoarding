namespace AOB.BAL.Common.Enums
{
    // Enums for "Payment Configuration".

    /// <summary>
    /// Card type the customer accepts (multiple choice).
    /// Stored comma-separated in ProjectPaymentConfiguration.SupportedCards.
    /// </summary>
    public enum CardScheme
    {
        Visa = 1,
        Mastercard = 2,
        AmericanExpress = 3,
        Discover = 4,
        Maestro = 5,
        DinersClub = 6,
        Jcb = 7,
        UnionPay = 8
    }

    /// <summary>
    /// Kind of payment transaction run through PCI Pal (multiple choice).
    /// Stored comma-separated in ProjectPaymentConfiguration.PaymentTypes.
    /// </summary>
    public enum PaymentType
    {
        Authorization = 1,
        CaptureSale = 2,
        Tokenization = 3
    }

    /// <summary>
    /// Currency customers pay in, as ISO 4217 codes (multiple choice).
    /// Stored comma-separated in ProjectPaymentConfiguration.Currencies.
    /// </summary>
    public enum CurrencyCode
    {
        GBP = 1,
        USD = 2,
        EUR = 3,
        AUD = 4,
        CAD = 5,
        CHF = 6,
        SEK = 7,
        NOK = 8,
        DKK = 9,
        JPY = 10,
        SGD = 11,
        AED = 12
    }

    /// <summary>How the card number is masked when displayed in the customer's systems (single choice).</summary>
    public enum CardNumberMasking
    {
        FullyMasked = 1,
        PartiallyMasked = 2,
        Last4 = 3
    }

    /// <summary>
    /// How PCI Pal tells the customer's merchant accounts apart. Asked when there is more than one merchant account
    /// (single choice).
    /// </summary>
    public enum MerchantIdentification
    {
        Currency = 1,
        Country = 2,
        Other = 3
    }

    /// <summary>Who provides the SMS service (single choice).</summary>
    public enum SmsServiceProvider
    {
        TwilioPciPalAccount = 1,
        Other = 2,

        /// <summary>SMS is not used. Standard: nothing needs setting up.</summary>
        None = 3
    }

    /// <summary>The customer's own SMS provider, asked when SmsServiceProvider is Other.</summary>
    public enum OtherSmsProvider
    {
        Vonage = 1,
        MessageBird = 2,
        Sinch = 3,
        Infobip = 4,
        Other = 5,

        /// <summary>The customer's own Twilio account (not PCI Pal's).</summary>
        Twilio = 6
    }

    /// <summary>Who provides the email service (single choice).</summary>
    public enum EmailServiceProvider
    {
        SendGridPciPalAccount = 1,
        Other = 2,

        /// <summary>Email is not used. Standard: nothing needs setting up.</summary>
        None = 3
    }

    /// <summary>The customer's own email provider, asked when EmailServiceProvider is Other.</summary>
    public enum OtherEmailProvider
    {
        SendGrid = 1,
        Mailgun = 2,
        AmazonSes = 3,
        Microsoft365 = 4,
        Postmark = 5,
        SparkPost = 6,
        Other = 7
    }
}
