using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Reception.Models;
using Reception.Database.Models;
using Reception.Interfaces;

namespace Reception.Controllers;

[Authorize]
[ApiController]
[Route("tags")]
[Produces("application/json")]
public class TagsController(ITagHandler handler) : ControllerBase
{
    /// <summary>
    /// Get all tags as a <see cref="TagDTO[]"/> of unique tag names.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<TagDTO>>> GetTags() => Ok(
        await handler.GetTags()
    );

    /// <summary>
    /// Get the <see cref="Tag"/> with Unique '<paramref ref="name"/>' (string)
    /// </summary>
    [HttpGet("name/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDTO>> GetTag(string name) =>
        await handler.GetTag(name);

    /// <summary>
    /// Get all tags (<see cref="Tag"/>) matching names in '<paramref ref="tagNames"/>' (string[])
    /// </summary>
    [HttpPost("name")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<TagDTO>>> GetTagsByNames([FromBody] IEnumerable<string> tagNames) =>
        await handler.GetTagsByNames(tagNames);


    /// <summary>
    /// Get the <see cref="Tag"/> with '<paramref ref="name"/>' (string) along with a collection of all associated Albums.
    /// </summary>
    /// <returns>
    /// <seealso cref="TagAlbumCollection"/>
    /// </returns>
    [HttpGet("name/{name}/albums")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagAlbumCollection>> GetTagAlbumCollection(string name) =>
        await handler.GetTagAlbums(name);

    /// <summary>
    /// Get the <see cref="Tag"/> with '<paramref ref="name"/>' (string) along with a collection of all associated Photos.
    /// </summary>
    /// <returns>
    /// <seealso cref="TagPhotoCollection"/>
    /// </returns>
    [HttpGet("name/{name}/photos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagPhotoCollection>> GetTagPhotoCollection(string name) =>
        await handler.GetTagPhotos(name);

    /// <summary>
    /// Create all non-existing tags in the '<paramref ref="tagNames"/>' (string[]) array.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status304NotModified)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<TagDTO>>> CreateTags([FromBody] IEnumerable<string> tagNames) =>
        await handler.CreateTags(tagNames);

    /// <summary>
    /// Update the properties of the <see cref="Tag"/> with '<paramref ref="name"/>' (string), *not* its members (i.e Photos or Albums).
    /// </summary>
    [HttpPut("name/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TagDTO>> UpdateTag(string name, MutateTag mut) =>
        await handler.UpdateTag(name, mut);

    /// <summary>
    /// Delete the <see cref="Tag"/> with '<paramref ref="name"/>' (string).
    /// </summary>
    [HttpDelete("name/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<IStatusCodeActionResult>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteTag(string name) =>
        await handler.DeleteTag(name);
}
