using System.ComponentModel.DataAnnotations;

namespace Verity.Application.Tags;

public sealed record TagRequest([MaxLength(500)] string? Reason);
