using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvaloniaCoreSnow
{
    public class SnowViewModel : INotifyPropertyChanged
    {
        private int _flakeCount = 3000;

        private Flake[] _flakes;

        private readonly Random _rnd = new Random();
        
        private readonly Action _invalidate;

        public SnowViewModel(Action invalidate)
        {
            _invalidate = invalidate;

            Bitmap = InitBitmap(640, 480);

            InitFlakes();
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

        private static unsafe WritableBitmap InitBitmap(int width, int height)
        {
            // Bgra8888 is device-native and much faster.
            var bmp = new WritableBitmap(width, height, PixelFormat.Bgra8888);

            // Draw on bottom line.
            using (var buf = bmp.Lock())
            {
                var ptr = (uint*) buf.Address;
                ptr += width * (height - 1);

                for (var i = 0; i < width; i++)
                {
                    *ptr = uint.MaxValue;
                    ptr++;
                }

            }

            return bmp;
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
            var tone = (byte) _rnd.Next(200);
            f.Color = GetGray((byte) (tone + 50));
            f.X = (short) _rnd.Next(Bitmap.PixelWidth);
            f.Speed = tone;
            f.Y = 0;
            f.Y2 = 0;
        }

        private unsafe void MoveFlakes()
        {
            const short slowdown = 200;

            while (true)
            {
                var bmp = Bitmap;
                var w = bmp.PixelWidth;

                using (var buf = bmp.Lock())
                {
                    var ptr = (uint*) buf.Address;

                    for (var i = 0; i < _flakes.Length; i++)
                    {
                        ref var f = ref _flakes[i];

                        f.Y2 += f.Speed;

                        if (f.Y2 > slowdown)
                        {
                            var oldPtr = ptr + w * f.Y + f.X;

                            // Erase old flake.
                            *oldPtr = 0;

                            var newPtr = oldPtr + w;
                            var newAlphaPtr = (byte*) newPtr + 3;

                            // Check snow below us.
                            if (*newAlphaPtr == byte.MaxValue)
                            {
                                if (f.X > 0 && *(newAlphaPtr - 4) != byte.MaxValue)
                                {
                                    f.X--;
                                    newPtr--;
                                }
                                else if (f.X + 1 < w && *(newAlphaPtr + 4) != byte.MaxValue)
                                {
                                    f.X++;
                                    newPtr++;
                                }
                                else
                                {
                                    InitFlake(ref f);
                                    newPtr = ptr + w * f.Y + f.X;

                                    // Mark as static by setting alpha to 255.
                                    // Make persistent color lighter.
                                    var clr = byte.MaxValue * 0.8 + f.Speed * 0.2;
                                    *oldPtr = GetGray((byte) clr) | 0xFF000000;
                                }
                            }
                            else
                            {
                                // Move.
                                f.Y2 = (short)(f.Y2 % slowdown);
                                f.Y++;
                            }

                            *newPtr = f.Color;
                        }
                    }
                }

                _invalidate();
                Thread.Sleep(10);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static uint GetGray(byte tone)
        {
            return (uint) (tone | tone << 8 | tone << 16 | 0xFE000000);
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
            public uint Color;
            public byte Speed;
        }
    }
}
