using System;
using Microsoft.DirectX.Direct3D;

namespace GarboDev.Graphics
{
    public class RenderTarget
    {
        private readonly Device _device;
        private Surface _surface;
        private readonly RenderToSurface _renderToSurface;
        private readonly Viewport _viewport;

        public int Width { get; }

        public int Height { get; }

        public Texture Texture { get; private set; }

        public RenderTarget(int width, int height, Device device)
        {
            _device = device;
            Width = width;
            Height = height;

            _viewport = new Viewport {X = 0, Y = 0, Width = width, Height = height};

            InitializeRenderTarget();

            _renderToSurface = new RenderToSurface(device, _surface.Description.Width, _surface.Description.Height,
                _surface.Description.Format, true, DepthFormat.D24S8);

            _renderToSurface.Reset += OnRenderToSurfaceReset;
        }

        private void InitializeRenderTarget()
        {
            Texture = new Texture(_device, Width, Height, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);

            _surface = Texture.GetSurfaceLevel(0);
        }

        private void OnRenderToSurfaceReset(object sender, EventArgs e)
        {
            InitializeRenderTarget();
        }

        public void BeginScene()
        {
            _renderToSurface.BeginScene(_surface, _viewport);
        }

        public void EndScene()
        {
            _renderToSurface.EndScene(Filter.None);
        }
    }
}
