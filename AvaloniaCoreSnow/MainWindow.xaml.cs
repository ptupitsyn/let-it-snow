using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvaloniaCoreSnow
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
        }

        private unsafe void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            //var c = new Image {Source = new Bitmap(PixelFormat.Rgba8888, IntPtr.Zero, 1, 1, 1)};

            Background = Brushes.Black;

            int w = 255, h = 255;
            int bpp = 4;
            int stride = w * bpp;


            // Show
            var img = (Image)Content;
            //img.Width = w * 2;
            //img.Height = h * 2;
            var bmp = new WritableBitmap(w, h, PixelFormat.Rgba8888); // TODO: Pixel format affects perf?
            img.Source = bmp;
            img.Stretch = Stretch.Fill;

            Task.Run(() =>
            {
                byte b = 0;
                while (true)
                {
                    Thread.Sleep(40);
                    using (var buf = bmp.Lock())
                    {
                        b += 1;
                        var ptr = (byte*) buf.Address;
                        for (int x = 0; x < w; x++)
                        {
                            for (int y = 0; y < h; y++)
                            {
                                var p = ptr + y * stride + x * bpp;

                                *p = (byte) (x % 255 + b);
                                *(p + 1) = (byte) (y % 255 + b);
                                *(p + 2) = b;
                                *(p + 3) = 255;
                            }
                        }
                    }

                    InvalidateVisual();

                    //Dispatcher.UIThread.InvokeTaskAsync(InvalidateVisual).Wait();
                }
            });


        }
    }
}
