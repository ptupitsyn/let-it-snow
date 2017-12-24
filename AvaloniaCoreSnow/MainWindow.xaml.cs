using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace AvaloniaCoreSnow
{
    public class MainWindow : Window
    {
        private readonly SnowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            this.AttachDevTools();

            _viewModel = new SnowViewModel(InvalidateVisual);
            DataContext = _viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            ((Image) Content).PointerMoved += Image_PointerMoved;
        }

        private void Image_PointerMoved(object sender, PointerEventArgs e)
        {
            if (e.InputModifiers.HasFlag(InputModifiers.LeftMouseButton))
            {
                var img = (Image) sender;
                var pos = e.GetPosition(img);

                var x = pos.X / img.Bounds.Width;
                var y = pos.Y / img.Bounds.Height;

                _viewModel.PutPixel(x, y, Colors.Red);
            }
        }
    }
}
