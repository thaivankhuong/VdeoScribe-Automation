using Whiteboard.Core.Models;

namespace Whiteboard.Core.Normalization;

public sealed record NormalizedVideoProject(
    VideoProject Project,
    string CanonicalJson);
