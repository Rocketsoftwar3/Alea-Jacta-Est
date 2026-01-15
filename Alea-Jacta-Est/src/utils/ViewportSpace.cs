using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Alea_Jacta_Est.utils
{
    public readonly struct ViewportSpace
    {
        private static Viewport _rootViewport;

        private readonly Rectangle _area;

        private ViewportSpace(Rectangle area)
        {
            _area = area;
        }

        public static void Bind(Viewport viewport)
        {
            _rootViewport = viewport;
        }

        public Vector2 At(float fx, float fy)
            => new Vector2(
                _area.X + _area.Width * fx,
                _area.Y + _area.Height * fy
            );
        
        public static ViewportSpace Top(ViewportSpace space, int num, int den)
            => ViewportSpace.Space(space).From(0, 1, 0, 1).To(1, 1, num, den);

        public static ViewportSpace Bottom(ViewportSpace space, int num, int den)
            => ViewportSpace.Space(space).From(0, 1, den - num, den).To(1, 1, 1, 1);

        public static Builder Viewport
            => new Builder(null);

        public static Builder Space(ViewportSpace space)
            => new Builder(space._area);

        public readonly struct Builder
        {
            private readonly Rectangle? _base;
            private readonly int _fxn, _fxd, _fyn, _fyd;

            public Builder(Rectangle? baseArea)
            {
                _base = baseArea;
                _fxn = _fxd = _fyn = _fyd = 0;
            }

            private Builder(
                Rectangle? baseArea,
                int fxn, int fxd,
                int fyn, int fyd)
            {
                _base = baseArea;
                _fxn = fxn;
                _fxd = fxd;
                _fyn = fyn;
                _fyd = fyd;
            }

            public Builder From(int xNum, int xDen)
                => new Builder(_base, xNum, xDen, 0, 1);

            public Builder From(int xNum, int xDen, int yNum, int yDen)
                => new Builder(_base, xNum, xDen, yNum, yDen);

            public ViewportSpace To(int xNum, int xDen)
                => To(xNum, xDen, 1, 1);

            public ViewportSpace To(int xNum, int xDen, int yNum, int yDen)
            {
                Rectangle root = _base ?? new Rectangle(
                    _rootViewport.X,
                    _rootViewport.Y,
                    _rootViewport.Width,
                    _rootViewport.Height
                );

                int x1 = root.X + root.Width * _fxn / _fxd;
                int y1 = root.Y + root.Height * _fyn / _fyd;

                int x2 = root.X + root.Width * xNum / xDen;
                int y2 = root.Y + root.Height * yNum / yDen;

                return new ViewportSpace(
                    new Rectangle(
                        x1,
                        y1,
                        x2 - x1,
                        y2 - y1
                    )
                );
            }
        }
    }
    
}
