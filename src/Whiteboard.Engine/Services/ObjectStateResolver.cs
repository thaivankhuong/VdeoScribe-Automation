using System.Collections.Generic;
using System.Linq;
using Whiteboard.Core.Enums;
using Whiteboard.Core.Models;
using Whiteboard.Engine.Context;
using Whiteboard.Engine.Models;
using Whiteboard.Engine.Resolvers;

namespace Whiteboard.Engine.Services;

public sealed class ObjectStateResolver : IObjectStateResolver
{
    public IReadOnlyList<ResolvedSceneState> Resolve(
        VideoProject project,
        FrameContext frameContext,
        IReadOnlyList<ResolvedTimelineEvent> timelineEvents)
    {
        return project.Scenes
            .Select(scene => new ResolvedSceneState
            {
                SceneId = scene.Id,
                Objects = scene.Objects
                    .Select(obj => new ResolvedObjectState
                    {
                        SceneObjectId = obj.Id,
                        Type = obj.Type,
                        AssetRefId = obj.AssetRefId,
                        TextContent = obj.TextContent,
                        Layer = obj.Layer,
                        IsVisible = obj.IsVisible,
                        RevealProgress = ResolveRevealProgress(obj.Id, obj.IsVisible, timelineEvents),
                        Transform = obj.Transform
                    })
                    .ToList()
            })
            .ToList();
    }

    private static double ResolveRevealProgress(
        string sceneObjectId,
        bool isVisible,
        IReadOnlyList<ResolvedTimelineEvent> timelineEvents)
    {
        var revealEvents = timelineEvents
            .Where(evt => evt.SceneObjectId == sceneObjectId)
            .Where(evt => evt.ActionType is TimelineActionType.Draw or TimelineActionType.Reveal)
            .ToList();

        if (revealEvents.Count == 0)
        {
            return isVisible ? 1 : 0;
        }

        return revealEvents.Any(evt => evt.IsActive) ? 1 : 0;
    }
}
