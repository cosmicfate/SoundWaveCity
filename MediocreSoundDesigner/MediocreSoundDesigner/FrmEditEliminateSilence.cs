namespace MediocreSoundDesigner;

public partial class FrmEditEliminateSilence : FrmEditMaster
{
    public FrmEditEliminateSilence()
    {
        InitializeComponent();
    }

    protected override void Init()
    {
        base.Init();
        cmbWhere.SelectedIndex = 0;
    }

    protected override void Apply()
    {
        SampleRegion region = cmbWhere.SelectedIndex switch
        {
            0 => SampleRegion.BEGINNING_ONLY,
            1 => SampleRegion.END_ONLY,
            2 => SampleRegion.BEGINNING_AND_END,
            3 => SampleRegion.ANYWHERE
        };
        track.EliminateSilence(region, (float)nudMinDuration.Value, (float)nudThreshold.Value);
    }
}
