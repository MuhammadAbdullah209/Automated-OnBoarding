using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// Issues and validates email-verification tokens and password-reset OTPs.
    /// Limits come from the "AuthConfig" section of appsettings.json.
    /// </summary>
    public class OtpService(IOtpTokenRepository otpTokenRepository, IUnitOfWork unitOfWork, IConfiguration configuration) : IOtpService
    {
        private readonly int _otpLength = configuration.GetValue("AuthConfig:OtpLength", 6);
        private readonly int _otpMaxAttempts = configuration.GetValue("AuthConfig:OtpMaxAttempts", 5);

        public int PasswordResetOtpExpiryMinutes { get; } = configuration.GetValue("AuthConfig:OtpExpiryMinutes", 10);

        public int EmailVerificationExpiryHours { get; } = configuration.GetValue("AuthConfig:EmailVerificationExpiryHours", 24);

        public async Task<string> CreateEmailVerificationTokenAsync(Guid userAuthId)
        {
            var token = SecurityTokenHelper.GenerateUrlSafeToken();
            await IssueAsync(userAuthId, OtpPurpose.EmailVerification, token, TimeSpan.FromHours(EmailVerificationExpiryHours));
            return token;
        }

        public async Task<Guid?> ConsumeEmailVerificationTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var record = await otpTokenRepository.GetByHashAsync(OtpPurpose.EmailVerification, SecurityTokenHelper.Hash(token));
            if (record is null || record.ConsumedAt is not null || record.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return null;
            }

            record.ConsumedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();
            return record.UserAuthId;
        }

        public async Task<string> CreatePasswordResetOtpAsync(Guid userAuthId)
        {
            var otp = SecurityTokenHelper.GenerateNumericOtp(_otpLength);
            await IssueAsync(userAuthId, OtpPurpose.PasswordReset, otp, TimeSpan.FromMinutes(PasswordResetOtpExpiryMinutes));
            return otp;
        }

        public async Task<bool> ConsumePasswordResetOtpAsync(Guid userAuthId, string otp)
        {
            var record = await otpTokenRepository.GetLatestActiveAsync(userAuthId, OtpPurpose.PasswordReset);
            if (record is null || record.FailedAttempts >= _otpMaxAttempts)
            {
                return false;
            }

            if (!SecurityTokenHelper.HashesMatch(record.TokenHash, SecurityTokenHelper.Hash(otp)))
            {
                // A 6-digit code is guessable, so wrong tries are counted and the code dies at the limit.
                record.FailedAttempts++;
                if (record.FailedAttempts >= _otpMaxAttempts)
                {
                    record.ConsumedAt = DateTimeOffset.UtcNow;
                }

                await unitOfWork.SaveChangesAsync();
                return false;
            }

            record.ConsumedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<string> CreatePasswordResetGrantAsync(Guid userAuthId)
        {
            var grant = SecurityTokenHelper.GenerateUrlSafeToken();
            await IssueAsync(userAuthId, OtpPurpose.PasswordResetGrant, grant, TimeSpan.FromMinutes(PasswordResetOtpExpiryMinutes));
            return grant;
        }

        public async Task<bool> ConsumePasswordResetGrantAsync(Guid userAuthId, string grant)
        {
            if (string.IsNullOrWhiteSpace(grant))
            {
                return false;
            }

            var record = await otpTokenRepository.GetLatestActiveAsync(userAuthId, OtpPurpose.PasswordResetGrant);
            if (record is null || !SecurityTokenHelper.HashesMatch(record.TokenHash, SecurityTokenHelper.Hash(grant)))
            {
                return false;
            }

            record.ConsumedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();
            return true;
        }

        private const int MaxIssuedPerHour = 5;

        public async Task<bool> IsPasswordResetThrottledAsync(Guid userAuthId)
            => await otpTokenRepository.CountCreatedSinceAsync(userAuthId, OtpPurpose.PasswordReset, DateTimeOffset.UtcNow.AddHours(-1)) >= MaxIssuedPerHour;

        public async Task<bool> IsEmailVerificationThrottledAsync(Guid userAuthId)
            => await otpTokenRepository.CountCreatedSinceAsync(userAuthId, OtpPurpose.EmailVerification, DateTimeOffset.UtcNow.AddHours(-1)) >= MaxIssuedPerHour;

        /// <summary>Stores the hash of a new secret, invalidating any earlier one for the same purpose.</summary>
        private async Task IssueAsync(Guid userAuthId, OtpPurpose purpose, string secret, TimeSpan lifetime)
        {
            await otpTokenRepository.InvalidateActiveAsync(userAuthId, purpose);

            await otpTokenRepository.AddAsync(new OtpToken
            {
                UserAuthId = userAuthId,
                Purpose = purpose,
                TokenHash = SecurityTokenHelper.Hash(secret),
                ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime)
            });

            await unitOfWork.SaveChangesAsync();
        }
    }
}
