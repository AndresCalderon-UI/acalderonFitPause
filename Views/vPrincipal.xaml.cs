namespace acalderonFitPause.Views;

public partial class vPrincipal : ContentPage
{
    public vPrincipal()
    {
        InitializeComponent();
    }

    private void Perfil_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new Views.vPerfil());
    }
}