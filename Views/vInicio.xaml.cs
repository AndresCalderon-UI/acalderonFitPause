namespace acalderonFitPause.Views;

public partial class vInicio : ContentPage
{
    public vInicio()
    {
        InitializeComponent();
    }

    private void Comienzo_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new Views.vLogReg());
    }
}