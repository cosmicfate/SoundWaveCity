namespace MediocreSoundDesigner;

public partial class FrmEditVolume : FrmEditMaster
{
    public FrmEditVolume()
    {
        InitializeComponent();
    }

    protected override void Init()
    {
        base.Init();
        nudVolume.Value = (decimal)(track.Volume * 100.0f);
    }

    protected override void Apply()
    {
        track.SetVolume((float)nudVolume.Value / 100.0f);
    }
}
