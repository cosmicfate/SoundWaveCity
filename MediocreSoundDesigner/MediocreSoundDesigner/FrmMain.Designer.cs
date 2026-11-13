namespace MediocreSoundDesigner
{
    partial class FrmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            panTracks = new Panel();
            lblPlayhead = new Label();
            lblDebug = new Label();
            ofdLoadFile = new OpenFileDialog();
            menu = new MenuStrip();
            menuFile = new ToolStripMenuItem();
            menuFileOpen = new ToolStripMenuItem();
            menuFileSave = new ToolStripMenuItem();
            menuFileSettings = new ToolStripMenuItem();
            menuEdit = new ToolStripMenuItem();
            menuItemCopy = new ToolStripMenuItem();
            menuItemCut = new ToolStripMenuItem();
            menuItemPaste = new ToolStripMenuItem();
            menuItemDelete = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            menuEditFlip = new ToolStripMenuItem();
            menuEditReverse = new ToolStripMenuItem();
            menuEditVolume = new ToolStripMenuItem();
            menuEditEliminateSilence = new ToolStripMenuItem();
            menuGen = new ToolStripMenuItem();
            menuGenRecord = new ToolStripMenuItem();
            menuGenNoise = new ToolStripMenuItem();
            menuGenGradientNoise = new ToolStripMenuItem();
            btnPlay = new Button();
            tmrUpdate = new System.Windows.Forms.Timer(components);
            sfdSaveFile = new SaveFileDialog();
            trkDrawTimescale = new TrackBar();
            colorDialog = new ColorDialog();
            panTracks.SuspendLayout();
            menu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkDrawTimescale).BeginInit();
            SuspendLayout();
            // 
            // panTracks
            // 
            panTracks.AutoScroll = true;
            panTracks.BackColor = Color.DimGray;
            panTracks.BorderStyle = BorderStyle.Fixed3D;
            panTracks.Controls.Add(lblPlayhead);
            panTracks.Location = new Point(10, 102);
            panTracks.Name = "panTracks";
            panTracks.Size = new Size(1580, 541);
            panTracks.TabIndex = 0;
            // 
            // lblPlayhead
            // 
            lblPlayhead.BackColor = Color.White;
            lblPlayhead.Location = new Point(39, -2);
            lblPlayhead.Name = "lblPlayhead";
            lblPlayhead.Size = new Size(5, 385);
            lblPlayhead.TabIndex = 9;
            // 
            // lblDebug
            // 
            lblDebug.Dock = DockStyle.Bottom;
            lblDebug.Font = new Font("Segoe UI", 20.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDebug.ForeColor = Color.FromArgb(255, 255, 192);
            lblDebug.Location = new Point(0, 746);
            lblDebug.Name = "lblDebug";
            lblDebug.Size = new Size(1604, 37);
            lblDebug.TabIndex = 1;
            // 
            // ofdLoadFile
            // 
            ofdLoadFile.FileName = "openFileDialog1";
            // 
            // menu
            // 
            menu.Items.AddRange(new ToolStripItem[] { menuFile, menuEdit, menuGen });
            menu.Location = new Point(0, 0);
            menu.Name = "menu";
            menu.Size = new Size(1604, 24);
            menu.TabIndex = 5;
            menu.Text = "menuStrip1";
            // 
            // menuFile
            // 
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuFileOpen, menuFileSave, menuFileSettings });
            menuFile.Name = "menuFile";
            menuFile.Size = new Size(37, 20);
            menuFile.Text = "&File";
            // 
            // menuFileOpen
            // 
            menuFileOpen.Name = "menuFileOpen";
            menuFileOpen.Size = new Size(125, 22);
            menuFileOpen.Text = "Open...";
            menuFileOpen.Click += menuFileOpen_Click;
            // 
            // menuFileSave
            // 
            menuFileSave.Name = "menuFileSave";
            menuFileSave.Size = new Size(125, 22);
            menuFileSave.Text = "Save...";
            menuFileSave.Click += menuFileSave_Click;
            // 
            // menuFileSettings
            // 
            menuFileSettings.Name = "menuFileSettings";
            menuFileSettings.Size = new Size(125, 22);
            menuFileSettings.Text = "Settings...";
            menuFileSettings.Click += menuFileSettings_Click;
            // 
            // menuEdit
            // 
            menuEdit.DropDownItems.AddRange(new ToolStripItem[] { menuItemCopy, menuItemCut, menuItemPaste, menuItemDelete, toolStripSeparator1, menuEditFlip, menuEditReverse, menuEditVolume, menuEditEliminateSilence });
            menuEdit.Name = "menuEdit";
            menuEdit.Size = new Size(39, 20);
            menuEdit.Text = "E&dit";
            // 
            // menuItemCopy
            // 
            menuItemCopy.Name = "menuItemCopy";
            menuItemCopy.Size = new Size(180, 22);
            menuItemCopy.Text = "Copy          Ctrl+C";
            menuItemCopy.Click += menuEditCopy_Click;
            // 
            // menuItemCut
            // 
            menuItemCut.Name = "menuItemCut";
            menuItemCut.Size = new Size(180, 22);
            menuItemCut.Text = "Cut             Ctrl+X";
            menuItemCut.Click += menuEditCut_Click;
            // 
            // menuItemPaste
            // 
            menuItemPaste.Name = "menuItemPaste";
            menuItemPaste.Size = new Size(180, 22);
            menuItemPaste.Text = "Paste          Ctrl+V";
            menuItemPaste.Click += menuEditPaste_Click;
            // 
            // menuItemDelete
            // 
            menuItemDelete.Name = "menuItemDelete";
            menuItemDelete.Size = new Size(180, 22);
            menuItemDelete.Text = "Delete         Del";
            menuItemDelete.Click += menuEditDelete_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(177, 6);
            // 
            // menuEditFlip
            // 
            menuEditFlip.Name = "menuEditFlip";
            menuEditFlip.Size = new Size(180, 22);
            menuEditFlip.Text = "Flip";
            menuEditFlip.Click += menuEditFlip_Click;
            // 
            // menuEditReverse
            // 
            menuEditReverse.Name = "menuEditReverse";
            menuEditReverse.Size = new Size(180, 22);
            menuEditReverse.Text = "Reverse";
            menuEditReverse.Click += menuEditReverse_Click;
            // 
            // menuEditVolume
            // 
            menuEditVolume.Name = "menuEditVolume";
            menuEditVolume.Size = new Size(180, 22);
            menuEditVolume.Text = "Alter Volume...";
            menuEditVolume.Click += menuEditVolume_Click;
            // 
            // menuEditEliminateSilence
            // 
            menuEditEliminateSilence.Name = "menuEditEliminateSilence";
            menuEditEliminateSilence.Size = new Size(180, 22);
            menuEditEliminateSilence.Text = "Eliminate Silence...";
            menuEditEliminateSilence.Click += menuEditEliminateSilence_Click;
            // 
            // menuGen
            // 
            menuGen.DropDownItems.AddRange(new ToolStripItem[] { menuGenRecord, menuGenNoise, menuGenGradientNoise });
            menuGen.Name = "menuGen";
            menuGen.Size = new Size(66, 20);
            menuGen.Text = "&Generate";
            // 
            // menuGenRecord
            // 
            menuGenRecord.Name = "menuGenRecord";
            menuGenRecord.Size = new Size(152, 22);
            menuGenRecord.Text = "Record...";
            menuGenRecord.Click += menuGenRecord_Click;
            // 
            // menuGenNoise
            // 
            menuGenNoise.Name = "menuGenNoise";
            menuGenNoise.Size = new Size(152, 22);
            menuGenNoise.Text = "Noise";
            menuGenNoise.Click += menuGenNoise_Click;
            // 
            // menuGenGradientNoise
            // 
            menuGenGradientNoise.Name = "menuGenGradientNoise";
            menuGenGradientNoise.Size = new Size(152, 22);
            menuGenGradientNoise.Text = "Gradient Noise";
            menuGenGradientNoise.Click += menuGenGradientNoise_Click;
            // 
            // btnPlay
            // 
            btnPlay.AutoSize = true;
            btnPlay.BackColor = Color.Transparent;
            btnPlay.BackgroundImage = Properties.Resources.play;
            btnPlay.BackgroundImageLayout = ImageLayout.Stretch;
            btnPlay.FlatAppearance.BorderSize = 0;
            btnPlay.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnPlay.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnPlay.FlatStyle = FlatStyle.Flat;
            btnPlay.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnPlay.Location = new Point(717, 661);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new Size(100, 100);
            btnPlay.TabIndex = 6;
            btnPlay.UseVisualStyleBackColor = false;
            btnPlay.Click += btnPlay_Click;
            // 
            // tmrUpdate
            // 
            tmrUpdate.Enabled = true;
            tmrUpdate.Interval = 20;
            tmrUpdate.Tick += tmrUpdatePlaybackState_Tick;
            // 
            // trkDrawTimescale
            // 
            trkDrawTimescale.LargeChange = 50;
            trkDrawTimescale.Location = new Point(12, 55);
            trkDrawTimescale.Maximum = 1000;
            trkDrawTimescale.Minimum = 1;
            trkDrawTimescale.Name = "trkDrawTimescale";
            trkDrawTimescale.Size = new Size(1580, 45);
            trkDrawTimescale.SmallChange = 10;
            trkDrawTimescale.TabIndex = 7;
            trkDrawTimescale.TickFrequency = 10;
            trkDrawTimescale.Value = 500;
            trkDrawTimescale.Scroll += trkDrawTimescale_Scroll;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(1604, 783);
            Controls.Add(trkDrawTimescale);
            Controls.Add(btnPlay);
            Controls.Add(lblDebug);
            Controls.Add(panTracks);
            Controls.Add(menu);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MainMenuStrip = menu;
            Name = "FrmMain";
            Text = "Sound Wave City";
            panTracks.ResumeLayout(false);
            menu.ResumeLayout(false);
            menu.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trkDrawTimescale).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panTracks;
    private Label lblDebug;
    private OpenFileDialog ofdLoadFile;
    private MenuStrip menu;
    private ToolStripMenuItem menuEdit;
    private Button btnPlay;
    private ToolStripMenuItem menuEditVolume;
    private ToolStripMenuItem menuEditReverse;
    private ToolStripMenuItem menuFile;
    private ToolStripMenuItem menuFileOpen;
    private ToolStripMenuItem menuFileSave;
    private System.Windows.Forms.Timer tmrUpdate;
    private ToolStripMenuItem menuGen;
    private ToolStripMenuItem menuGenNoise;
    private ToolStripMenuItem menuGenGradientNoise;
    private ToolStripMenuItem menuEditFlip;
    private SaveFileDialog sfdSaveFile;
    private TrackBar trkDrawTimescale;
    private ColorDialog colorDialog;
    private ToolStripMenuItem menuEditEliminateSilence;
    private Label lblPlayhead;
    private ToolStripMenuItem menuGenRecord;
    private ToolStripMenuItem menuFileSettings;
        private ToolStripMenuItem menuItemCopy;
        private ToolStripMenuItem menuItemCut;
        private ToolStripMenuItem menuItemPaste;
        private ToolStripMenuItem menuItemDelete;
        private ToolStripSeparator toolStripSeparator1;
    }
}
