using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvaloniaCoreSnow
{
    public class MainWindow : Window
    {
        private SnowViewModel _viewModel;
        private IControl _img;

        public MainWindow()
        {
            InitializeComponent();
            this.AttachDevTools();

            DataContext = _viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoaderPortableXaml.Load(this);

            _img = ((Grid) Content).Children.First();
            _img.PointerMoved += Image_PointerMoved;

            // Delegate is called from bg thread, use synchronous call to avoid concurrency issues within Avalonia.
            _viewModel = new SnowViewModel(() =>
                Dispatcher.UIThread.InvokeTaskAsync(() => _img.InvalidateVisual()).Wait());
        }

        private void Image_PointerMoved(object sender, PointerEventArgs e)
        {
            if (e.InputModifiers.HasFlag(InputModifiers.LeftMouseButton))
            {
                var pos = e.GetPosition(_img);

                var x = pos.X / _img.Bounds.Width;
                var y = pos.Y / _img.Bounds.Height;

                _viewModel.PutPixel(x, y, Colors.Red, 2);
            }
        }
    }
}
