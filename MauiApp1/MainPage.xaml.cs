namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnStartButtonClick(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ProductsPage());
        }
    }

}
