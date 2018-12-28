using System;
using Microsoft.DirectX.Direct3D;

namespace GarboDev.Graphics
{
    public class RenderTarget
    {
        private Device device = null;
        private int width;
        private int height;
        private Texture texture = null;
        private Surface surface = null;
        private RenderToSurface renderToSurface = null;
        private Viewport viewport;

        public int Width
        {
            get { return this.width; }
        }

        public int Height
        {
            get { return this.height; }
        }

        public Texture Texture
        {
            get { return this.texture; }
        }

        public RenderTarget(int width, int height, Device device)
        {
            this.device = device;
            this.width = width;
            this.height = height;

            viewport = new Viewport();
            viewport.X = 0;
            viewport.Y = 0;
            viewport.Width = width;
            viewport.Height = height;

            this.InitializeRenderTarget();

            this.renderToSurface = new RenderToSurface(device, surface.Description.Width, surface.Description.Height,
                surface.Description.Format, true, DepthFormat.D24S8);

            this.renderToSurface.Reset += new EventHandler(OnRenderToSurfaceReset);
        }

        private void InitializeRenderTarget()
        {
            this.texture = new Texture(device, width, height, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);

            this.surface = texture.GetSurfaceLevel(0);
        }

        private void OnRenderToSurfaceReset(object sender, EventArgs e)
        {
            this.InitializeRenderTarget();
        }

        public void BeginScene()
        {
            this.renderToSurface.BeginScene(this.surface, this.viewport);
        }

        public void EndScene()
        {
            this.renderToSurface.EndScene(Filter.None);
        }
    }
}
