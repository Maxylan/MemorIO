using System.Text.Json.Serialization;

namespace MemorIO.Database.Models;

/// <summary>
/// The <see cref="LogEntry"/> data transfer object (DTO).
/// </summary>
public class LogEntryDTO : LogEntry, IDataTransferObject<LogEntry>
{
    [JsonPropertyName("id")]
    public new int? Id { get; set; }

    /*
    [JsonPropertyName("user_id")]
    public new int? UserId { get; set; }

    [JsonPropertyName("user_email")]
    public new string? UserEmail { get; set; }

    [JsonPropertyName("user_username")]
    public new string? UserUsername { get; set; }

    [JsonPropertyName("user_full_name")]
    public new string? UserFullName { get; set; }

    [JsonPropertyName("request_address")]
    public new string? RequestAddress { get; set; }

    [JsonPropertyName("request_user_agent")]
    public new string? RequestUserAgent { get; set; }

    [JsonPropertyName("created_at")]
    public new DateTime CreatedAt { get; set; }

    [JsonPropertyName("action")]
    public new string Action { get; set; } = null!;

    [JsonPropertyName("message")]
    public new string? Message { get; set; }
    */

    /*
    [JsonIgnore, SwaggerIgnore]
    public LogFormat Format => new(this);

    /// <summary>
    /// Set the <see cref="MemorIO.Database.Models.Method"/> of this entity using a string (<paramref name="method"/>)
    /// </summary>
    public void SetMethod(string? method) => this.Method = method?.ToUpper() switch
    {
        "HEAD" => MemorIO.Database.Method.HEAD,
        "GET" => MemorIO.Database.Method.GET,
        "POST" => MemorIO.Database.Method.POST,
        "PUT" => MemorIO.Database.Method.PUT,
        "PATCH" => MemorIO.Database.Method.PATCH,
        "DELETE" => MemorIO.Database.Method.DELETE,
        _ => MemorIO.Database.Method.UNKNOWN
    };
    */

    /// <summary>
    /// Convert this <see cref="LogEntryDTO"/> instance to its <see cref="LogEntry"/> equivalent.
    /// </summary>
    public LogEntry ToEntity() => new() {
        Id = this.Id ?? default,
        UserId = this.UserId,
        UserEmail  = this.UserEmail,
        UserUsername  = this.UserUsername,
        UserFullName = this.UserFullName,
        RequestAddress  = this.RequestAddress,
        RequestUserAgent  = this.RequestUserAgent,
        CreatedAt  = this.CreatedAt,
        Action  = this.Action,
        Message = this.Message
    };

    /// <summary>
    /// Compare this <see cref="LogEntryDTO"/> against its <see cref="LogEntry"/> equivalent.
    /// </summary>
    public bool Equals(LogEntry entity) {
        throw new NotImplementedException();
    }
}
