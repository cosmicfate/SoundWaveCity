namespace MediocreSoundDesigner;


public partial class FrmMain
{
    private const int CLIP_MENU_OFFSET_X = 14;


    private ContextMenuStrip clipMenu = null!;      // shown when a clip is right-clicked
    private ContextMenuStrip trackMenu = null!;     // shown when an empty part of a track is right-clicked
    // the track that the open menu is acting on.
    private Track clipMenuTrack;

    // ADD NEW CLIP EDITS HERE -- if we make new effects 
    private void RegisterClipEdits()
    {
        AddClipEdit("Split", track => track.SplitAt(scrubbedXPosition));
        AddClipEdit("Reverse", track => track.Reverse());
        AddClipEdit("Flip", track => track.Flip());
    }

    private void InitializeClipContextMenu()
    {
        clipMenu = new ContextMenuStrip();
        RegisterClipEdits();

        Image deleteIcon = SystemIcons.GetStockIcon(StockIconId.Delete, 16).ToBitmap();

        clipMenu.Items.Add(new ToolStripSeparator());
        // reuse delete functions
        clipMenu.Items.Add("Delete Clip", deleteIcon, (_, _) => DeleteSelectedClip(clipMenuTrack));

        trackMenu = new ContextMenuStrip();
        trackMenu.Items.Add("Delete Track", deleteIcon, (_, _) => DeleteTrack(clipMenuTrack));

        // the playhead line sits on top of the tracks, so it gets the right-click instead of
        // the track under it. right-clicking it opens the menu for the clip under the playhead
        // WITHOUT moving the playhead.
        lblPlayhead.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;


            // y is mouse y relative to the top of the form
            int y = lblPlayhead.Top + e.Y;
            // find the track under the playhead, and select the clip under the playhead on that track
            Track track = trackManager.Tracks.Find(t =>
                y >= t.PanDrawSamples.Top && y < t.PanDrawSamples.Bottom);
            if (track is null) return;

            scrubbedXPosition = lblPlayhead.Left;

            trackManager.SelectTrack(track);
            track.SetDrawTimescale(trackManager.DrawTimescale);
            track.SelectClipAt(scrubbedXPosition);
            track.PanDrawSamples.Invalidate();

            ShowTrackContextMenu(track, lblPlayhead, e.Location);
        };
    }

    private void AddClipEdit(string text, Action<Track> edit)
    {
        clipMenu.Items.Add(text, null, (_, _) =>
        {
            if (clipMenuTrack?.SelectedClip is null) return;
            edit(clipMenuTrack);
            clipMenuTrack.PanDrawSamples?.Invalidate();
        });
    }

    // opens a menu next to the cursor
    private void ShowTrackContextMenu(Track track, Control clicked, Point location)
    {
        clipMenuTrack = track;
        ContextMenuStrip menu = track.SelectedClip is not null ? clipMenu : trackMenu;
        menu.Show(clicked, new Point(location.X + CLIP_MENU_OFFSET_X, location.Y));
    }
}
