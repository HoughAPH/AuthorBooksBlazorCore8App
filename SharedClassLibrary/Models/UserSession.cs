namespace SharedClassLibrary.Models;

// A simple record to pass user claims around statelessly
public record UserSession(
    string Id,
    string Name,
    string Email,
    string Role
);
