using MediocreSoundDesigner.Code;

namespace MediocreSoundDesigner;

public partial class FrmMain
{
    // store the clip, its original track, and where the mouse grabbed it.
    private sealed class ClipDragData
    {
        public Track Source { get; }
        public Clip Clip { get; }
        public int GrabOffset { get; }
        public bool Cancelled { get; set; }
        public bool VisitedTarget { get; set; }
        public string FailureReason { get; set; } = "";

        public ClipDragData(Track source, Clip clip, int grabOffset)
        {
            Source = source;
            Clip = clip;
            GrabOffset = grabOffset;
        }
    }

    // edge snapping is based on screen pixels, not the number of audio samples.
    private const int ClipSnapPixels = 20;

    // attach mouse and drag events to each existing waveform panel.
    private void EnableClipDragDrop(Track track, Panel panel)
    {
        panel.AllowDrop = true;
        ClipDragData candidate = null;
        Point mouseDownAt = Point.Empty;

        panel.MouseDown += (_, e) =>
        {
            candidate = null;
            if (e.Button != MouseButtons.Left) return;
            if (trackManager.State == NAudio.Wave.PlaybackState.Playing) return;

            track.SetDrawTimescale(trackManager.DrawTimescale);
            Clip clip = track.ClipAt(e.X);
            if (clip is null) return;

            trackManager.SelectTrack(track);
            track.SelectClipAt(e.X);
            panel.Invalidate();

            int grabOffset = track.SamplePositionAt(e.X) - clip.StartSample;
            candidate = new ClipDragData(track, clip, grabOffset);
            mouseDownAt = e.Location;
        };

        panel.MouseMove += (_, e) =>
        {
            if (candidate is null || (e.Button & MouseButtons.Left) == 0) return;

            // wait for a real drag rather than treating a click as a clip move.
            Size threshold = SystemInformation.DragSize;
            Rectangle safeArea = new(
                mouseDownAt.X - threshold.Width / 2,
                mouseDownAt.Y - threshold.Height / 2,
                threshold.Width,
                threshold.Height);
            if (safeArea.Contains(e.Location)) return;

            ClipDragData activeDrag = candidate;
            candidate = null;

            // escape cancels the operation without displaying a failure message.
            QueryContinueDragEventHandler cancelHandler = (_, args) =>
            {
                if (args.Action == DragAction.Cancel)
                    activeDrag.Cancelled = true;
            };

            panel.QueryContinueDrag += cancelHandler;
            try
            {
                DragDropEffects result = panel.DoDragDrop(activeDrag, DragDropEffects.Move);

                // a rejected drop does not fire dragdrop, so report its reason here.
                if (result == DragDropEffects.None && !activeDrag.Cancelled &&
                    activeDrag.VisitedTarget && !string.IsNullOrWhiteSpace(activeDrag.FailureReason))
                {
                    MessageBox.Show(activeDrag.FailureReason, "Clip move not allowed",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            finally
            {
                panel.QueryContinueDrag -= cancelHandler;
            }
        };

        panel.MouseUp += (_, _) => candidate = null;
        panel.DragEnter += (_, e) => CheckClipDrop(track, panel, e);
        panel.DragOver += (_, e) => CheckClipDrop(track, panel, e);
        panel.DragLeave += (_, _) => RestoreTrackBackground(track, panel);
        panel.DragDrop += (_, e) =>
        {
            RestoreTrackBackground(track, panel);
            if (!TryGetClipDrop(track, panel, e, out ClipDragData data,
                out int newStart, out string reason))
            {
                if (data is not null)
                    MessageBox.Show(reason, "Clip move not allowed",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!trackManager.MoveClip(data.Source, track, data.Clip,
                newStart, out reason))
            {
                MessageBox.Show(reason, "Clip move failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
    }

    // indicate whether the destination can accept the current dragged clip.
    private void CheckClipDrop(Track target, Panel panel, DragEventArgs e)
    {
        bool allowed = TryGetClipDrop(target, panel, e,
            out ClipDragData drag, out _, out string reason);
        if (drag is not null)
        {
            drag.VisitedTarget = true;
            drag.FailureReason = allowed ? "" : reason;
        }

        e.Effect = allowed ? DragDropEffects.Move : DragDropEffects.None;
        Color background = allowed || trackManager.SelectedTrack == target
            ? Settings.Color.TrackBg_Selected
            : Settings.Color.TrackBg_Unselected;
        if (panel.BackColor != background) panel.BackColor = background;
    }

    // find a valid destination before allowing the source clip to be removed.
    private bool TryGetClipDrop(Track target, Panel panel, DragEventArgs e,
        out ClipDragData drag, out int startSample, out string reason)
    {
        drag = e.Data?.GetData(typeof(ClipDragData)) as ClipDragData;
        startSample = 0;
        reason = "";
        if (drag is null) return false;

        Point point = panel.PointToClient(new Point(e.X, e.Y));
        if (!panel.ClientRectangle.Contains(point))
        {
            reason = "release the clip inside a track";
            return false;
        }

        target.SetDrawTimescale(trackManager.DrawTimescale);
        int channels = Math.Max(1, target.channelCount);
        Clip draggedClip = drag.Clip;

        // shift always puts the clip directly after the final other clip.
        if ((e.KeyState & 4) != 0)
        {
            long end = target.Clips
                .Where(clip => !ReferenceEquals(clip, draggedClip))
                .Select(clip => (long)clip.EndSample)
                .DefaultIfEmpty(0L).Max();

            if (!TryAlignSample(end, channels, out startSample))
            {
                reason = "the destination position is outside the supported range";
                return false;
            }
            return trackManager.CanIMoveClip(drag.Source, target,
                drag.Clip, startSample, out reason);
        }

        // raw placement retains the position where the clip was grabbed.
        long rawStart = (long)target.SamplePositionAt(point.X) - drag.GrabOffset;
        bool rawInRange = TryAlignSample(rawStart, channels, out int proposed);
        bool rawAllowed = rawInRange && trackManager.CanIMoveClip(
            drag.Source, target, drag.Clip, proposed, out _);

        // snap when a clip edge or the pointer approaches another clip's edge.
        // when dropped directly on an occupied clip, try its nearest free edge.
        int timescale = Math.Max(1, trackManager.DrawTimescale);
        long snapDistance = (long)ClipSnapPixels * timescale;
        int bestStart = -1;
        long bestScore = long.MaxValue;

        foreach (Clip neighbor in target.Clips)
        {
            // a clip must never attempt to snap to itself during a same-track move.
            if (ReferenceEquals(neighbor, drag.Clip)) continue;

            bool pointerInside = (long)point.X * timescale >= neighbor.StartSample &&
                (long)point.X * timescale < neighbor.EndSample;
            long[] boundaries = { neighbor.EndSample,
                (long)neighbor.StartSample - drag.Clip.Samples.Length };
            long[] pointerEdges = { neighbor.EndSample, neighbor.StartSample };

            for (int i = 0; i < boundaries.Length; i++)
            {
                long edge = pointerEdges[i];
                long pointerDistance = Math.Abs((long)point.X * timescale - edge);
                long startDistance = rawInRange
                    ? Math.Abs((long)proposed - boundaries[i]) : long.MaxValue;

                if (!pointerInside && pointerDistance > snapDistance &&
                    startDistance > snapDistance) continue;

                if (!TryAlignSample(boundaries[i], channels, out int candidate)) continue;
                if (!trackManager.CanIMoveClip(drag.Source, target,
                    drag.Clip, candidate, out _)) continue;

                // a pointer already inside a neighboring clip favors its nearest edge.
                long score = pointerInside ? pointerDistance
                    : Math.Min(pointerDistance, startDistance);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestStart = candidate;
                }
            }
        }

        // a nearby valid snap takes priority; otherwise use the requested location.
        if (bestStart >= 0)
        {
            startSample = bestStart;
            return true;
        }

        if (!rawInRange)
        {
            reason = "the drop position is outside the track";
            return false;
        }

        startSample = proposed;
        if (rawAllowed) return true;
        return trackManager.CanIMoveClip(drag.Source, target,
            drag.Clip, startSample, out reason);
    }

    // convert a sample position to the nearest complete audio frame.
    private static bool TryAlignSample(long position, int channels, out int aligned)
    {
        aligned = 0;
        if (position < 0 || position > int.MaxValue) return false;
        position -= position % Math.Max(1, channels);
        aligned = (int)position;
        return true;
    }

    // undo temporary highlighting without changing the selected track.
    private void RestoreTrackBackground(Track track, Panel panel)
    {
        Color background = trackManager.SelectedTrack == track
            ? Settings.Color.TrackBg_Selected
            : Settings.Color.TrackBg_Unselected;
        if (panel.BackColor != background) panel.BackColor = background;
    }
}
