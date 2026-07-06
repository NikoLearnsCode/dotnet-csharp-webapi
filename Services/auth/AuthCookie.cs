namespace WebApi.Services.Auth;

// Single source of truth for the auth cookies, shared by login, refresh and
// logout so the attributes never drift apart. Lifetimes come from JwtOptions.
//
// The caller decides `secure` (see AuthController.SecureCookies): in Development
// it follows the request scheme (CookieSecurePolicy.SameAsRequest semantics) so
// strict non-browser clients like Insomnia work over plain http://localhost;
// outside Development it must always be true - deriving it from IsHttps there
// would silently drop the flag behind a TLS-terminating proxy.
public static class AuthCookie
{
    public const string Name = "jwt";
    public const string RefreshName = "refreshToken";

    // The refresh token is only ever needed by the auth endpoints; scoping the
    // cookie keeps it out of every other request.
    public const string RefreshPath = "/api/auth";

    public static CookieOptions AccessOptions(TimeSpan lifetime, bool secure) =>
        Base(lifetime, secure);

    public static CookieOptions RefreshOptions(TimeSpan lifetime, bool secure)
    {
        var options = Base(lifetime, secure);
        options.Path = RefreshPath;
        return options;
    }

    private static CookieOptions Base(TimeSpan lifetime, bool secure) =>
        new()
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.Add(lifetime),
        };
}
