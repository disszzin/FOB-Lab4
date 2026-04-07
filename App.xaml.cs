using Microsoft.Extensions.DependencyInjection;

namespace FisheryMAUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell())
            {
                Title = "Fisheries Oceanology Base: Lab 4",
                Width = 1360,
                Height = 920,
                MinimumWidth = 960,
                MinimumHeight = 760,
                MaximumWidth = 1500,
                MaximumHeight = 1100
            };
        }
    }
}
