using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace AuthPractice.Core.Auth;

public static class IdentityErrorsExtensions
{
    // IdentityError (DuplicateEmail, PasswordTooShort, ...) → наши Errors (400)
    public static Errors ToErrors(this IEnumerable<IdentityError> errors) =>
        errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();
}
