namespace IdentityManagerServerApi.Features.Account;

/// <summary>Delivers a password-reset token to its owner. Swap in an email implementation when one exists.</summary>
public interface IResetTokenSender
{
    Task Send(string email, string token);
}

/// <summary>Development only: writes the token to the log so it can be pasted into the reset page.</summary>
public class LoggingResetTokenSender(ILogger<LoggingResetTokenSender> logger) : IResetTokenSender
{
    public Task Send(string email, string token)
    {
        logger.LogWarning("DEV ONLY - password reset token for {Email}: {Token}", email, token);
        return Task.CompletedTask;
    }
}

/// <summary>Used when no real sender is configured: the token is never exposed.</summary>
public class NoResetTokenSender(ILogger<NoResetTokenSender> logger) : IResetTokenSender
{
    public Task Send(string email, string token)
    {
        logger.LogWarning("Password reset requested for {Email} but no reset token sender is configured.", email);
        return Task.CompletedTask;
    }
}
