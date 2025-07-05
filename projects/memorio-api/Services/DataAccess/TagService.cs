using System.Net;
using Microsoft.AspNetCore.Mvc;
using MemorIO.Middleware.Authentication;
using Microsoft.EntityFrameworkCore;
using MemorIO.Interfaces.DataAccess;
using MemorIO.Interfaces;
using MemorIO.Database.Models;
using MemorIO.Database;
using MemorIO.Models;

namespace MemorIO.Services.DataAccess;

public class TagService(
    ILoggingService<TagService> logging,
    IHttpContextAccessor contextAccessor,
    MageDb db
) : ITagService
{
    /// <summary>
    /// Get all tags.
    /// </summary>
    public async Task<IEnumerable<Tag>> GetTags(int? offset = null, int? limit = 9999)
    {
        IQueryable<Tag> tagsQuery = db.Tags
            .Include(tag => tag.UsedByAlbums)
            .Include(tag => tag.UsedByPhotos);

        if (offset is not null) {
            tagsQuery = tagsQuery
                .Skip(offset.Value);
        }

        if (limit is not null) {
            tagsQuery = tagsQuery
                .Take(limit.Value);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return [];
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(GetTags))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return [];
        }

        var tags = await tagsQuery // Filter by privilege
            .Where(tag => (user.Privilege & (tag.RequiredPrivilege & (Privilege.VIEW | Privilege.VIEW_ALL))) == (tag.RequiredPrivilege & (Privilege.VIEW | Privilege.VIEW_ALL)))
            .ToArrayAsync();

        return tags
            .OrderBy(tag => tag.Name)
            .OrderByDescending(tag => tag.Items);
    }

    /// <summary>
    /// Get the <see cref="Tag"/> with Unique '<paramref ref="name"/>' (string)
    /// </summary>
    public async Task<ActionResult<Tag>> GetTag(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            string message = $"{nameof(Tag)} names cannot be null/empty.";
            logging
                .Action(nameof(GetTag))
                .InternalDebug(message)
                .LogAndEnqueue();

            return new BadRequestObjectResult(message);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(GetTag))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        name = name.Trim();
        if (!name.IsNormalized()) {
            name = name.Normalize();
        }

        if (name.Length > 127)
        {
            string message = $"{nameof(Tag)} name exceeds maximum allowed length of 127.";
            logging
                .Action(nameof(GetTag))
                .InternalDebug(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new BadRequestObjectResult(message);
        }

        var tag = await db.Tags
            .Include(tag => tag.UsedByPhotos)
            .Include(tag => tag.UsedByAlbums)
            .FirstOrDefaultAsync(tag => tag.Name == name);

        if (tag is null)
        {
            string message = $"{nameof(Tag)} with name '{name}' could not be found!";

            if (Program.IsDevelopment)
            {
                logging
                    .Action(nameof(GetTag))
                    .InternalDebug(message, opts => {
                        opts.SetUser(user);
                    })
                    .LogAndEnqueue();
            }

            return new NotFoundObjectResult(message);
        }

        byte requiredViewPrivilege = (byte)
            (tag.RequiredPrivilege & (Privilege.VIEW | Privilege.VIEW_ALL));

        if ((user.Privilege & requiredViewPrivilege) != requiredViewPrivilege)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({requiredViewPrivilege}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(GetTag))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        return tag;
    }

    /// <summary>
    /// Get the <see cref="Tag"/> with Primary Key '<paramref ref="tagId"/>' (int)
    /// </summary>
    public async Task<ActionResult<Tag>> GetTagById(int tagId)
    {
        if (tagId == default)
        {
            string message = $"{nameof(Tag)} ID has to be a non-zero positive integer!";
            logging
                .Action(nameof(GetTag))
                .InternalDebug(message)
                .LogAndEnqueue();

            return new BadRequestObjectResult(message);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(GetTagById))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        var tag = await db.Tags
            .FindAsync(tagId);

        if (tag is null)
        {
            string message = $"{nameof(Tag)} with ID #{tagId} could not be found!";

            if (Program.IsDevelopment)
            {
                logging
                    .Action(nameof(GetTagById))
                    .InternalDebug(message, opts => {
                        opts.SetUser(user);
                    })
                    .LogAndEnqueue();
            }

            return new NotFoundObjectResult(message);
        }

        foreach(var navigation in db.Entry(tag).Navigations)
        {
            if (!navigation.IsLoaded) {
                await navigation.LoadAsync();
            }
        }

        byte requiredViewPrivilege = (byte)
            (tag.RequiredPrivilege & (Privilege.VIEW | Privilege.VIEW_ALL));

        if ((user.Privilege & requiredViewPrivilege) != requiredViewPrivilege)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({requiredViewPrivilege}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(GetTagById))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        return tag;
    }

    /// <summary>
    /// Get the <see cref="Tag"/> with '<paramref ref="name"/>' (string) along with a collection of all associated Albums.
    /// </summary>
    public async Task<ActionResult<TagAlbumCollection>> GetTagAlbums(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            string message = $"{nameof(Tag)} names cannot be null/empty.";
            logging
                .Action(nameof(GetTagAlbums))
                .InternalDebug(message)
                .LogAndEnqueue();

            return new BadRequestObjectResult(message);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(GetTagAlbums))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        name = name.Trim();
        if (!name.IsNormalized()) {
            name = name.Normalize();
        }

        if (name.Length > 127)
        {
            string message = $"{nameof(Tag)} name exceeds maximum allowed length of 127.";
            logging
                .Action(nameof(GetTagAlbums))
                .InternalDebug(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new BadRequestObjectResult(message);
        }

        var tag = await db.Tags
            .Include(tag => tag.UsedByAlbums)
            .FirstOrDefaultAsync(tag => tag.Name == name);

        if (tag is null)
        {
            string message = $"{nameof(Tag)} with name '{name}' could not be found!";

            if (Program.IsDevelopment)
            {
                logging
                    .Action(nameof(GetTagAlbums))
                    .InternalDebug(message, opts => {
                        opts.SetUser(user);
                    })
                    .LogAndEnqueue();
            }

            return new NotFoundObjectResult(message);
        }

        if (tag.UsedByAlbums is null || tag.UsedByAlbums.Count <= 0)
        {
            if (Program.IsDevelopment) {
                logging.Logger.LogDebug($"Tag {tag.Name} has no associated albums.");
            }

            // Initialize with a collection that's at least empty.
            tag.UsedByAlbums = [];
        }

        byte requiredViewPrivilege = (byte)
            (tag.RequiredPrivilege & (Privilege.VIEW | Privilege.VIEW_ALL));

        if ((user.Privilege & requiredViewPrivilege) != requiredViewPrivilege)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({requiredViewPrivilege}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(GetTagAlbums))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        var albums = await db.Albums
            .Include(album => album.Tags)
            .Where(album => album.Tags.Any(t => t.TagId == tag.Id))
            .Select(album => album.DTO())
            .ToListAsync();

        return new TagAlbumCollection()
        {
            Tag = tag.DTO(),
            Albums = albums
        };
    }

    /// <summary>
    /// Get the <see cref="Tag"/> with '<paramref ref="name"/>' (string) along with a collection of all associated Photos.
    /// </summary>
    public async Task<ActionResult<TagPhotoCollection>> GetTagPhotos(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            string message = $"{nameof(Tag)} names cannot be null/empty.";
            logging
                .Action(nameof(GetTagPhotos))
                .InternalDebug(message)
                .LogAndEnqueue();

            return new BadRequestObjectResult(message);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(GetTagPhotos))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        name = name.Trim();
        if (!name.IsNormalized()) {
            name = name.Normalize();
        }

        if (name.Length > 127)
        {
            string message = $"{nameof(Tag)} name exceeds maximum allowed length of 127.";
            logging
                .Action(nameof(GetTagPhotos))
                .InternalDebug(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new BadRequestObjectResult(message);
        }

        var tag = await db.Tags
            .Include(tag => tag.UsedByPhotos)
            .FirstOrDefaultAsync(tag => tag.Name == name);

        if (tag is null)
        {
            string message = $"{nameof(Tag)} with name '{name}' could not be found!";

            if (Program.IsDevelopment)
            {
                logging
                    .Action(nameof(GetTagPhotos))
                    .InternalDebug(message, opts => {
                        opts.SetUser(user);
                    })
                    .LogAndEnqueue();
            }

            return new NotFoundObjectResult(message);
        }

        if (tag.UsedByPhotos is null || tag.UsedByPhotos.Count <= 0)
        {
            if (Program.IsDevelopment) {
                logging.Logger.LogDebug($"Tag {tag.Name} has no associated photos.");
            }

            // Initialize with a collection that's at least empty.
            tag.UsedByAlbums = [];
        }

        byte requiredViewPrivilege = (byte)
            (tag.RequiredPrivilege & (Privilege.VIEW | Privilege.VIEW_ALL));

        if ((user.Privilege & requiredViewPrivilege) != requiredViewPrivilege)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({requiredViewPrivilege}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(GetTagPhotos))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        var photos = await db.Photos
            .Include(photo => photo.Tags)
            .Where(photo => photo.Tags.Any(t => t.TagId == tag.Id))
            .Select(photo => photo.DTO())
            .ToListAsync();

        return new TagPhotoCollection()
        {
            Tag = tag.DTO(),
            Photos = photos
        };
    }

    /// <summary>
    /// Get all tags (<see cref="Tag"/>) matching names in '<paramref ref="tagNames"/>' (string[])
    /// </summary>
    public async Task<ActionResult<IEnumerable<Tag>>> GetTagsByNames(IEnumerable<string> tagNames)
    {
        if (tagNames.Count() > 9999)
        {
            tagNames = tagNames
                .Take(9999);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(GetTagsByNames))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if ((user.Privilege & Privilege.VIEW) != Privilege.VIEW)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({Privilege.VIEW}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(GetTagsByNames))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        tagNames = tagNames
            .Where(tn => !string.IsNullOrWhiteSpace(tn))
            .Where(tn => tn.Length < 128)
            .Distinct();

        if (tagNames.Count() <= 0)
        {
            return new OkObjectResult(Array.Empty<Tag>());
        }

        var tags = await db.Tags
            .Where(tag => tagNames.Contains(tag.Name))
            .ToArrayAsync();

        return tags;
    }

    /// <summary>
    /// Create all non-existing tags in the '<paramref ref="tagNames"/>' (string[]) array.
    /// </summary>
    public async Task<ActionResult<IEnumerable<Tag>>> CreateTags(IEnumerable<string> tagNames)
    {
        if (tagNames.Count() > 9999)
        {
            tagNames = tagNames
                .Take(9999);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(CreateTags))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if ((user.Privilege & Privilege.CREATE) != Privilege.CREATE)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({Privilege.CREATE}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(CreateTags))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        IEnumerable<ITag> tags = tagNames
            .Where(tn => !string.IsNullOrWhiteSpace(tn))
            .Where(tn => tn.Length < 128)
            .Distinct()
            .Select(tn => tn.Normalize().Trim())
            .Select(tn => tn.Replace(' ', '_'))
            .Select(name => new TagDTO {
                Name = name,
                RequiredPrivilege = Privilege.NONE
            });

        if (tags.Count() <= 0)
        {
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }

        return await this.CreateTags(tags);
    }

    /// <summary>
    /// Create all non-existing tags in the '<paramref ref="tags"/>' (<see cref="IEnumerable{ITag}"/>) array.
    /// </summary>
    public async Task<ActionResult<IEnumerable<Tag>>> CreateTags(IEnumerable<ITag> tags)
    {
        if (tags.Count() > 9999)
        {
            tags = tags
                .Take(9999);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(CreateTags))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        byte requiredPrivilege = (byte)
            (Privilege.CREATE | Privilege.UPDATE);

        if ((user.Privilege & requiredPrivilege) != requiredPrivilege)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({requiredPrivilege}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(CreateTags))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        int successfullyAddedTags = 0;
        int successfullyUpdatedTags = 0;
        List<Tag> finishedTags = [];
        Stack<ITag> validTags = (Stack<ITag>)tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag.Name) && tag.Name.Length < 128)
            .Where(tag => string.IsNullOrEmpty(tag.Description) || tag.Description.Length < 32767)
            .Where(tag => (user.Privilege & tag.RequiredPrivilege) == tag.RequiredPrivilege)
            .Distinct();

        if (validTags.Count <= 0)
        {
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }

        int i = 0;
        while (validTags.Any() && ++i < 9999)
        {
            ITag validTag = validTags.Pop();
            validTag.Name = validTag.Name
                .Normalize()
                .Trim()
                .Replace(' ', '_');

            Tag? tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == validTag.Name);
            bool tagExisted = tag is not null;

            int? idSnapshot = tag?.Id;
            string? nameSnapshot = tag?.Name;
            string? descriptionSnapshot = tag?.Description;
            byte? privilegeSnapshot = tag?.RequiredPrivilege;

            if (!tagExisted)
            {
                tag = new() {
                    Id = default,
                    Name = validTag.Name,
                    Description = null, /* validTag.Description */
                    RequiredPrivilege = validTag.RequiredPrivilege
                };
            }

            if (!string.IsNullOrEmpty(validTag.Description))
            {
                tag!.Description = validTag.Description!
                    .Normalize()
                    .Trim();
            }

            if (validTag.RequiredPrivilege != tag!.RequiredPrivilege)
            {
                if ((user.Privilege & Privilege.ADMIN) != Privilege.ADMIN)
                {
                    string message = $"Prevented action with 'RequiredPrivilege' ({Privilege.ADMIN}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
                    logging
                        .Action(nameof(CreateTags))
                        .ExternalSuspicious(message, opts => {
                            opts.SetUser(user);
                        })
                        .LogAndEnqueue();

                    return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                }

                tag!.RequiredPrivilege = validTag.RequiredPrivilege;
            }

            finishedTags.Add(tag);

            if (tagExisted)
            {
                if (
                    tag.Id == idSnapshot &&
                    tag.Name == nameSnapshot &&
                    tag.Description == descriptionSnapshot &&
                    tag.RequiredPrivilege == privilegeSnapshot
                ) {
                    if (Program.IsDevelopment)
                    {
                        logging.Logger.LogDebug($"Skipping updating tag '{tag.Name}' (#{tag.Id}) because no change had been made to it.");
                    }

                    continue;
                }

                db.Tags.Update(tag);
                successfullyUpdatedTags++;
            }
            else
            {
                db.Tags.Add(tag);
                successfullyAddedTags++;
            }
        }

        if (successfullyAddedTags <= 0 ||
            successfullyUpdatedTags <= 0
        ) {
            if (Program.IsDevelopment)
            {
                logging.Logger.LogDebug($"Skipping saving database as no tags where created/updated.");
            }

            return finishedTags;
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException updateException)
        {
            string message = $"Cought a {nameof(DbUpdateException)} attempting to add/update '{finishedTags.Count}' new tags. ";
            logging
                .Action(nameof(CreateTags))
                .InternalError(message + " " + updateException.Message, opts =>
                {
                    opts.Exception = updateException;
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(message + (
                Program.IsProduction ? HttpStatusCode.InternalServerError.ToString() : updateException.Message
            ))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (Exception ex)
        {
            string message = $"Cought an unkown exception of type '{ex.GetType().FullName}' while attempting to add/upate '{finishedTags.Count}' new tags. ";
            logging
                .Action(nameof(CreateTags))
                .InternalError(message + " " + ex.Message, opts =>
                {
                    opts.Exception = ex;
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(message + (
                Program.IsProduction ? HttpStatusCode.InternalServerError.ToString() : ex.Message
            ))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        return finishedTags;
    }

    /// <summary>
    /// Update the properties of the <see cref="Tag"/> with '<paramref ref="name"/>' (string), *not* its members (i.e Photos or Albums).
    /// </summary>
    public async Task<ActionResult<Tag>> UpdateTag(string existingTagName, MutateTag mut)
    {
        ArgumentNullException.ThrowIfNull(mut);

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(UpdateTag))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if ((user.Privilege & Privilege.UPDATE) != Privilege.UPDATE)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({Privilege.UPDATE}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(UpdateTag))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if (string.IsNullOrWhiteSpace(mut.Name))
        {
            string message = $"{nameof(Tag)} names cannot be null/empty.";
            logging
                .Action(nameof(UpdateTag))
                .InternalDebug(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new BadRequestObjectResult(message);
        }

        mut.Name = mut.Name.Trim();
        if (!mut.Name.IsNormalized()) {
            mut.Name = mut.Name.Normalize();
        }

        if (mut.Name.Length > 127)
        {
            string message = $"{nameof(Tag)} name exceeds maximum allowed length of 127.";
            logging
                .Action(nameof(UpdateTag))
                .InternalDebug(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new BadRequestObjectResult(message);
        }

        Tag? tag = null;
        if (mut.Id is not null && mut.Id != 0) {
            tag = await db.Tags.FindAsync(mut.Id);
        }

        if (tag is null)
        {
            var getTag = await GetTag(existingTagName);
            tag = getTag.Value;

            if (tag is null)
            {
                string message = $"{nameof(Tag)} named '{existingTagName}' or identified by ID #{mut.Id} could not be found!";

                // A failure *should* already be logged at `GetTag`
                if (Program.IsDevelopment)
                {
                    logging
                        .Action(nameof(UpdateTag))
                        .InternalDebug(message, opts => {
                            opts.SetUser(user);
                        })
                        .LogAndEnqueue();
                }
                else {
                    logging.Logger.LogDebug(message);
                }

                return getTag.Result!;
            }
        }

        tag.Name = mut.Name;
        tag.Description = mut.Description;

        try
        {
            db.Update(tag);
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException updateException)
        {
            string message = $"Cought a {nameof(DbUpdateException)} attempting to update {nameof(Tag)} '{existingTagName}'. ";
            logging
                .Action(nameof(UpdateTag))
                .InternalError(message + " " + updateException.Message, opts =>
                {
                    opts.Exception = updateException;
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(message + (
                Program.IsProduction ? HttpStatusCode.InternalServerError.ToString() : updateException.Message
            ))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (Exception ex)
        {
            string message = $"Cought an unkown exception of type '{ex.GetType().FullName}' while attempting to update {nameof(Tag)} '{existingTagName}'. ";
            logging
                .Action(nameof(UpdateTag))
                .InternalError(message + " " + ex.Message, opts =>
                {
                    opts.Exception = ex;
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(message + (
                Program.IsProduction ? HttpStatusCode.InternalServerError.ToString() : ex.Message
            ))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        return tag;
    }

    /// <summary>
    /// Edit tags associated with a <see cref="Album"/> identified by PK <paramref name="albumId"/>.
    /// </summary>
    public async Task<ActionResult<(Album, IEnumerable<Tag>)>> MutateAlbumTags(int albumId, IEnumerable<ITag> tags)
    {
        if (albumId <= 0) {
            throw new ArgumentException($"Parameter {nameof(albumId)} has to be a non-zero positive integer!", nameof(albumId));
        }

        if (tags.Count() > 9999)
        {
            tags = tags
                .Take(9999);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(MutateAlbumTags))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if ((user.Privilege & Privilege.UPDATE) != Privilege.UPDATE)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({Privilege.UPDATE}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(MutateAlbumTags))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        Album? album = await db.Albums.FindAsync(albumId);

        if (album is null)
        {
            string message = $"Failed to find a {nameof(Photo)} with {nameof(albumId)} #{albumId}.";
            logging
                .Action(nameof(MutateAlbumTags))
                .LogDebug(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new NotFoundObjectResult(
                Program.IsProduction ? HttpStatusCode.NotFound.ToString() : message
            );
        }

        // Load missing navigation entries.
        foreach (var navigation in db.Entry(album).Navigations)
        {
            if (!navigation.IsLoaded)
            {
                await navigation.LoadAsync();
            }
        }

        var albumTagsResult = await this.MutateAlbumTags(album, tags);
        var albumTags = albumTagsResult.Value;

        if (albumTags is null) {
            return albumTagsResult.Result!;
        }

        return (album, albumTags);
    }

    /// <summary>
    /// Edit tags associated with the <paramref name="album"/> (<see cref="Album"/>).
    /// </summary>
    public async Task<ActionResult<IEnumerable<Tag>>> MutateAlbumTags(Album album, IEnumerable<ITag> tags)
    {
        ArgumentNullException.ThrowIfNull(album, nameof(album));

        if (tags.Count() > 9999)
        {
            tags = tags
                .Take(9999);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(MutateAlbumTags))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if ((user.Privilege & Privilege.UPDATE) != Privilege.UPDATE)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({Privilege.UPDATE}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(MutateAlbumTags))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        var createAndSanitizeTags = await CreateTags(tags);
        var validTags = createAndSanitizeTags.Value;

        if (validTags is null /* || createAndSanitizeTags.Result is not OkObjectResult */) {
            return createAndSanitizeTags.Result!;
        }

        album.Tags = validTags
            .Select(tag => new AlbumTagRelation() {
                Tag = tag,
                Added = DateTime.Now
            })
            .ToList();

        try
        {
            db.Update(album);

            logging
                .Action(nameof(MutateAlbumTags))
                .LogTrace(
                    $"The tags associated with {nameof(Album)} '{album.Title}' (#{album.Id}) was just updated.",
                    opts => {
                        opts.SetUser(user);
                    }
                )
                .LogAndEnqueue();

            await db.SaveChangesAsync();
        }
        catch (DbUpdateException updateException)
        {
            string message = $"Cought a {nameof(DbUpdateException)} attempting to update the tags of a {nameof(Album)} '{album.Title}' (#{album.Id}). ";
            logging
                .Action(nameof(MutateAlbumTags))
                .InternalError(message + " " + updateException.Message, opts =>
                {
                    opts.Exception = updateException;
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(message + (
                Program.IsProduction ? HttpStatusCode.InternalServerError.ToString() : updateException.Message
            ))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (Exception ex)
        {
            string message = $"Cought an unkown exception of type '{ex.GetType().FullName}' while attempting to update tags of a {nameof(Album)} '{album.Title}' (#{album.Id}). ";
            logging
                .Action(nameof(MutateAlbumTags))
                .InternalError(message + ex.Message, opts =>
                {
                    opts.Exception = ex;
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(message + (
                Program.IsProduction ? HttpStatusCode.InternalServerError.ToString() : ex.Message
            ))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        var albumTags = album.Tags
            .Select(relation => relation.Tag)
            .ToArray();

        return albumTags;
    }

    /// <summary>
    /// Edit tags associated with a <see cref="Photo"/> identified by PK <paramref name="photoId"/>.
    /// </summary>
    public async Task<ActionResult<(Photo, IEnumerable<Tag>)>> MutatePhotoTags(int photoId, IEnumerable<ITag> tags)
    {
        if (photoId <= 0) {
            throw new ArgumentException($"Parameter {nameof(photoId)} has to be a non-zero positive integer!", nameof(photoId));
        }

        if (tags.Count() > 9999)
        {
            tags = tags
                .Take(9999);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(MutatePhotoTags))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if ((user.Privilege & Privilege.UPDATE) != Privilege.UPDATE)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({Privilege.UPDATE}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(MutatePhotoTags))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        Photo? photo = await db.Photos.FindAsync(photoId);

        if (photo is null)
        {
            string message = $"Failed to find a {nameof(Photo)} with {nameof(photoId)} #{photoId}.";
            logging
                .Action(nameof(MutatePhotoTags))
                .LogDebug(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new NotFoundObjectResult(
                Program.IsProduction ? HttpStatusCode.NotFound.ToString() : message
            );
        }

        // Load missing navigation entries.
        foreach (var navigation in db.Entry(photo).Navigations)
        {
            if (!navigation.IsLoaded)
            {
                await navigation.LoadAsync();
            }
        }

        var createAndSanitizeTags = await CreateTags(tags);
        var validTags = createAndSanitizeTags.Value;

        if (validTags is null) {
            return createAndSanitizeTags.Result!;
        }

        foreach(var navigation in db.Entry(photo).Navigations)
        {
            if (!navigation.IsLoaded) {
                await navigation.LoadAsync();
            }
        }

        var photoTagsResult = await this.MutatePhotoTags(photo, tags);
        var photoTags = photoTagsResult.Value;

        if (photoTags is null) {
            return photoTagsResult.Result!;
        }

        return (photo, photoTags);
    }

    /// <summary>
    /// Edit tags associated with the <paramref name="photo"/> (<see cref="Photo"/>).
    /// </summary>
    public async Task<ActionResult<IEnumerable<Tag>>> MutatePhotoTags(Photo photo, IEnumerable<ITag> tags)
    {
        ArgumentNullException.ThrowIfNull(photo, nameof(photo));

        if (tags.Count() > 9999)
        {
            tags = tags
                .Take(9999);
        }

        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(MutatePhotoTags))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if ((user.Privilege & Privilege.UPDATE) != Privilege.UPDATE)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({Privilege.UPDATE}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(MutatePhotoTags))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        var createAndSanitizeTags = await CreateTags(tags);
        var validTags = createAndSanitizeTags.Value;

        if (validTags is null) {
            return createAndSanitizeTags.Result!;
        }

        photo.Tags = validTags
            .Select(tag => new PhotoTagRelation() {
                Tag = tag,
                Added = DateTime.Now
            })
            .ToList();

        try
        {
            db.Update(photo);

            logging
                .Action(nameof(MutatePhotoTags))
                .LogTrace(
                    $"The tags associated with {nameof(Photo)} '{photo.Title}' (#{photo.Id}) was just updated.",
                    opts => {
                        opts.SetUser(user);
                    }
                )
                .LogAndEnqueue();

            await db.SaveChangesAsync();
        }
        catch (DbUpdateException updateException)
        {
            string message = $"Cought a {nameof(DbUpdateException)} attempting to update the tags of a {nameof(Photo)} '{photo.Title}' (#{photo.Id}). ";
            logging
                .Action(nameof(MutatePhotoTags))
                .InternalError(message + updateException.Message, opts =>
                {
                    opts.Exception = updateException;
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(message + (
                Program.IsProduction ? HttpStatusCode.InternalServerError.ToString() : updateException.Message
            ))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (Exception ex)
        {
            string message = $"Cought an unkown exception of type '{ex.GetType().FullName}' while attempting to update tags of a {nameof(Photo)} '{photo.Title}' (#{photo.Id}). ";
            logging
                .Action(nameof(MutatePhotoTags))
                .InternalError(message + ex.Message, opts =>
                {
                    opts.Exception = ex;
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(message + (
                Program.IsProduction ? HttpStatusCode.InternalServerError.ToString() : ex.Message
            ))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        var photoTags = photo.Tags
            .Select(relation => relation.Tag)
            .ToArray();

        return photoTags;
    }

    /// <summary>
    /// Delete the <see cref="Tag"/> with '<paramref ref="name"/>' (string).
    /// </summary>
    public async Task<ActionResult> DeleteTag(string name)
    {
        Account? user;
        try
        {
            user = MemoAuth.GetAccount(contextAccessor);

            if (user is null) {
                return new ObjectResult("Prevented attempted unauthorized access.") {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
        catch (Exception ex)
        {
            string message = $"Cought an '{ex.GetType().FullName}' invoking {nameof(MemoAuth.GetAccount)}!";
            logging
                .Action(nameof(DeleteTag))
                .ExternalError(message, opts => { opts.Exception = ex; })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if ((user.Privilege & Privilege.DELETE) != Privilege.DELETE)
        {
            string message = $"Prevented action with 'RequiredPrivilege' ({Privilege.DELETE}), which exceeds the user's 'Privilege' of ({user.Privilege}).";
            logging
                .Action(nameof(DeleteTag))
                .ExternalSuspicious(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(Program.IsProduction ? HttpStatusCode.Forbidden.ToString() : message) {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            string message = $"{nameof(Tag)} names cannot be null/empty.";
            logging
                .Action(nameof(DeleteTag))
                .LogDebug(message, opts => {
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new BadRequestObjectResult(message);
        }

        name = name.Trim();
        if (!name.IsNormalized()) {
            name = name.Normalize();
        }

        var getTag = await GetTag(name);
        Tag? tag = getTag.Value;

        if (tag is null)
        {
            string message = $"{nameof(Tag)} named '{name}' could not be found!";

            // A failure *should* already be logged at `GetTag`
            if (Program.IsDevelopment)
            {
                logging
                    .Action(nameof(DeleteTag))
                    .LogDebug(message, opts => {
                        opts.SetUser(user);
                    })
                    .LogAndEnqueue();
            }
            else {
                logging.Logger.LogDebug(message);
            }

            return getTag.Result!;
        }

        try
        {
            db.Remove(tag);
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException updateException)
        {
            string message = $"Cought a {nameof(DbUpdateException)} attempting to delete {nameof(Tag)} '{name}'. ";
            logging
                .Action(nameof(DeleteTag))
                .InternalError(message + " " + updateException.Message, opts =>
                {
                    opts.Exception = updateException;
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(message + (
                Program.IsProduction ? HttpStatusCode.InternalServerError.ToString() : updateException.Message
            ))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        catch (Exception ex)
        {
            string message = $"Cought an unkown exception of type '{ex.GetType().FullName}' while attempting to delete {nameof(Tag)} '{name}'. ";
            logging
                .Action(nameof(DeleteTag))
                .InternalError(message + " " + ex.Message, opts =>
                {
                    opts.Exception = ex;
                    opts.SetUser(user);
                })
                .LogAndEnqueue();

            return new ObjectResult(message + (
                Program.IsProduction ? HttpStatusCode.InternalServerError.ToString() : ex.Message
            ))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        return new NoContentResult();
    }
}
