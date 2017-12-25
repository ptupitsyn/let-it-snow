using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvaloniaCoreSnow
{
    public class SnowViewModel : INotifyPropertyChanged
    {
        private const byte MaxSpeed = 200;

        private int _flakeCount = 3000;

        private Flake[] _flakes;

        private readonly Random _rnd = new Random();
        
        private readonly Action _invalidate;

        private int _delayMs = 10;

        public SnowViewModel(Action invalidate)
        {
            _invalidate = invalidate;

            ResetCommand = new DelegateCommand(Reset);

            // Bgra8888 is device-native and much faster.
            Bitmap = new WritableBitmap(640, 480, PixelFormat.Bgra8888);
            Reset();
            Task.Run(() => MoveFlakes());
        }

        public WritableBitmap Bitmap { get; }

        public int FlakeCount
        {
            get => _flakeCount;
            set
            {
                _flakeCount = value;
                OnPropertyChanged(nameof(FlakeCount));
            }
        }

        public int DelayMs
        {
            get => _delayMs;
            set { _delayMs = value; OnPropertyChanged(nameof(DelayMs)); }
        }

        public ICommand ResetCommand { get; }

        public unsafe void PutPixel(double x, double y, Color color, int size)
        {
            // Convert relative to absolute.
            var width = Bitmap.PixelWidth;
            var height = Bitmap.PixelHeight;

            var px = (int) (x * width);
            var py = (int) (y * height);

            var pixel = color.B + ((uint) color.G << 8) + ((uint) color.R << 16) + ((uint) byte.MaxValue << 24);

            for (var x0 = px - size; x0 <= px + size; x0++)
            for (var y0 = py - size; y0 <= py + size; y0++)
            {
                if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
                {
                    using (var buf = Bitmap.Lock())
                    {
                        var ptr = (uint*) buf.Address;
                        ptr += (uint) (width * y0 + x0);

                        *ptr = pixel;
                    }
                }
            }
        }

        private void Reset()
        {
            InitFlakes();
            ResetBitmap();
            DelayMs = 10;
        }

        private void InitFlakes()
        {
            _flakes = new Flake[_flakeCount];

            for (var i = 0; i < _flakes.Length; i++)
            {
                ref var f = ref _flakes[i];
                InitFlake(ref f);
                f.Y = (short) _rnd.Next(40);
                f.Y2 = 0;
            }
        }

        private void InitFlake(ref Flake f)
        {
            var tone = (byte) _rnd.Next(MaxSpeed);
            f.X = (short) _rnd.Next(Bitmap.PixelWidth);
            f.Speed = tone;
            f.Y = 0;
            f.Y2 = 0;
        }

        private unsafe void ResetBitmap()
        {
            using (var buf = Bitmap.Lock())
            {
                var ptr = (uint*)buf.Address;

                var w = Bitmap.PixelWidth;
                var h = Bitmap.PixelHeight;

                // Clear.
                for (var i = 0; i < w * (h - 1); i++)
                {
                    *(ptr + i) = 0;
                }

                // Draw bottom line.
                for (var i = w * (h - 1); i < w * h ; i++)
                {
                    *(ptr + i) = uint.MaxValue;
                }
            }
        }

        private unsafe void MoveFlakes()
        {
            while (true)
            {
                var bmp = Bitmap;
                var w = bmp.PixelWidth;

                using (var buf = bmp.Lock())
                {
                    var ptr = (uint*) buf.Address;

                    for (var i = 0; i < _flakes.Length; i++)
                    {
                        MoveFlake(ref _flakes[i], ptr, w);
                    }
                }

                _invalidate();
                Thread.Sleep(DelayMs);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private unsafe void MoveFlake(ref Flake f, uint* ptr, int width)
        {
            f.Y2 += f.Speed;

            const short slowdown = 200;
            if (f.Y2 < slowdown)
            {
                return;
            }

            // Erase old flake.
            var oldPtr = ptr + width * f.Y + f.X;
            *oldPtr = 0;

            // New position.
            f.Y2 = (short)(f.Y2 % slowdown);
            f.Y++;
            var newPtr = oldPtr + width;
            var newAlphaPtr = (byte*) newPtr + 3;

            // Check snow below us.
            if (*newAlphaPtr == byte.MaxValue)
            {
                // Check pixels to the left or to the right: we might be on a slope.
                if (f.X > 0 && *(newAlphaPtr - 4) != byte.MaxValue)
                {
                    f.X--;
                    newPtr--;
                }
                else if (f.X + 1 < width && *(newAlphaPtr + 4) != byte.MaxValue)
                {
                    f.X++;
                    newPtr++;
                }
                else
                {
                    // Not on a slope, stop here and preserve the pixel.
                    InitFlake(ref f);
                    newPtr = ptr + width * f.Y + f.X;

                    // Mark as static by setting alpha to 255.
                    // Make persistent color lighter.
                    var clr = MaxSpeed * 0.8 + f.Speed * 0.2;
                    *oldPtr = GetGray((byte) clr) | 0xFF000000;
                }
            }

            *newPtr = GetGray(f.Speed);
        }

        private static uint GetGray(byte tone)
        {
            var c = (byte) (byte.MaxValue - MaxSpeed + tone);

            // Non-max alpha indicates moving pixel.
            return (uint) (c | c << 8 | c << 16 | 0xFE000000);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private struct Flake
        {
            public short X;
            public short Y;
            public short Y2;
            public byte Speed;
        }
    }
}
