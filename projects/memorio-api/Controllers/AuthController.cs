using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using MemorIOAuthorizationService = MemorIO.Interfaces.IAuthorizationService;
using MemorIO.Interfaces.DataAccess;
using MemorIO.Models;
using MemorIO.Database.Models;
using MemorIO.Database;

namespace MemorIO.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController(
    MemorIOAuthorizationService authorization,
    ISessionService sessions
    ) : ControllerBase
{
    /// <summary>
    /// Validates that a session (..inferred from `<see cref="HttpContext"/>`) ..exists and is valid.
    /// In other words this endpoint tests my Authentication Pipeline.
    /// </summary>
    [Authorize]
    [HttpHead("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IStatusCodeActionResult ValidateSession() =>
        StatusCode(StatusCodes.Status200OK);


    /// <summary>
    /// Returns the `<see cref="Account"/>` tied to the requesting client's session (i.e, in our `<see cref="HttpContext"/>` pipeline).
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AccountDTO>> Me()
    {
        var sessionValidation = await authorization.ValidateSession(Source.EXTERNAL);
        var session = sessionValidation.Value;

        if (session is null || string.IsNullOrWhiteSpace(session.Code))
        {
            return sessionValidation.Result!;
        }

        if (session.Account is Account user)
        {
            return user.DTO();
        }

        var getAccount = await sessions.GetUserBySession(session);
        var account = getAccount.Value;

        if (account is null)
        {
            return getAccount.Result!;
        }

        return account.DTO();
    }

    /// <summary>
    /// Attempt to grab a full `<see cref="Session"/>` instance, identified by PK (uint) <paramref name="id"/>.
    /// </summary>
    [Authorize]
    [HttpGet("session/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionDTO>> GetSessionDetails([FromRoute] int id)
    {
        await sessions.CleanupSessions(); // Do a little cleaning first..
        var getSession = await sessions.GetSessionById(id);
        var session = getSession.Value;

        if (session is null)
        {
            return getSession.Result!;
        }

        return session.DTO();
    }


    /// <summary>
    /// Attempt to grab a full `<see cref="Session"/>` instance, identified by unique <paramref name="session"/> code (string).
    /// </summary>
    [Authorize]
    [HttpGet("session/code/{session}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionDTO>> GetSessionDetailsByCode([FromRoute] string session)
    {
        await sessions.CleanupSessions(); // Do a little cleaning first..
        var getSession = await sessions.GetSession(session);
        var sessionObj = getSession.Value;

        if (sessionObj is null)
        {
            return getSession.Result!;
        }

        return sessionObj.DTO();
    }

    /// <summary>
    /// Attempt to login a user, creating a new `<see cref="Session"/>` instance.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status408RequestTimeout)]
    public async Task<ActionResult<SessionDTO>> Login([FromBody] Login body) {
        var getSession = await authorization.Login(body.Username, body.Hash);
        if (getSession.Value is null) {
            return getSession.Result!;
        }

        var dto = getSession.Value.DTO();
        if (dto.Account.Sessions is not null) {
            dto.Account.Sessions = Array.Empty<Session>();
        }
        if (dto.Account.Clients is not null) {
            dto.Account.Clients = Array.Empty<Client>();
        }
        if (dto.Client.Sessions is not null) {
            dto.Client.Sessions = Array.Empty<SessionDTO>();
        }
        if (dto.Client.Accounts is not null) {
            dto.Client.Accounts = Array.Empty<AccountDTO>();
        }
        
        return dto;
    }
}
