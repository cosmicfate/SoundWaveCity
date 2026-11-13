using MediocreSoundDesigner.Code;

namespace MediocreSoundDesigner;
public interface IUndoableAction
{
    void Undo();
}

public class TrackSnapshotAction : IUndoableAction
{
    private readonly Track track;
    private readonly List<Clip> savedClips;
    private readonly int selectedClipIndex;
    private readonly float savedVolume;
    private readonly bool savedIsDisabled;

    public TrackSnapshotAction(Track track)
    {
        // Create a deep copy of the clips to capture their state at this moment.
        this.track = track;
        this.savedClips = track.Clips.Select(c => new Clip((float[])c.Samples.Clone(), c.StartSample)).ToList();
        this.selectedClipIndex = track.SelectedClip is null ? -1 : track.Clips.ToList().IndexOf(track.SelectedClip);
        this.savedVolume = track.Volume;
        this.savedIsDisabled = track.IsDisabled;
    }
    public void Undo()
    {
        track.RestoreSnapshot(savedClips, selectedClipIndex, savedVolume, savedIsDisabled);
    }
}

public class CompositeAction : IUndoableAction
{
    private readonly List<IUndoableAction> actions;

    public CompositeAction(params IUndoableAction[] actions)
    {
        this.actions = actions.Where(a => a is not null).ToList();
    }

    public void Undo()
    {
        for (int i = actions.Count - 1; i >= 0; i--)
        {
            actions[i].Undo();
        }
    }
}

public class TrackListAction : IUndoableAction
{
    private readonly TrackManager trackManager;
    private readonly Panel panContainer;
    private readonly Track track;
    private readonly int index;
    private readonly bool wasAdded;

    public TrackListAction(TrackManager trackManager, Panel panContainer, Track track, int index, bool wasAdded)
    {
        this.trackManager = trackManager;
        this.panContainer = panContainer;
        this.track = track;
        this.index = index;
        this.wasAdded = wasAdded;
    }

    public void Undo()
    {
        if (wasAdded)
        {
            if (track.PanDrawSamples is not null)
            {
                panContainer.Controls.Remove(track.PanDrawSamples);
            }
        }
        else
        {
            int insertIndex = Math.Clamp(index, 0, trackManager.Tracks.Count);
            trackManager.Tracks.Insert(insertIndex, track);
            if (track.PanDrawSamples is not null && !panContainer.Controls.Contains(track.PanDrawSamples))
            {
                panContainer.Controls.Add(track.PanDrawSamples);
            }
            trackManager.SelectTrack(track);
        }
    }
}

public class UndoMan
{
    private readonly Stack<IUndoableAction> undoStack = new();
    private const int MaxHistory = 30;

    public bool CanUndo => undoStack.Count > 0;
    public event Action HistoryChanged;

    public void Record(IUndoableAction action)
    {
        if (action is null) return;

        if (undoStack.Count >= MaxHistory)
        {
            var items = undoStack.ToArray();
            undoStack.Clear();
            for (int i = items.Length - 2; i >= 0; i--)
            {
                undoStack.Push(items[i]);
            }
        }

        undoStack.Push(action);
        HistoryChanged?.Invoke();
    }

    public void Undo()
    {
        if (!CanUndo) return;

        IUndoableAction action = undoStack.Pop();
        action.Undo();
        HistoryChanged?.Invoke();
    }

    public void Clear()
    {
        undoStack.Clear();
        HistoryChanged?.Invoke();
    }
}