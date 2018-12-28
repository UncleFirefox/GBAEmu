using System;
using System.Collections.Generic;
using System.IO;
using GarboDev.CrossCutting;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GarboDev.Graphics
{
    public class ShaderRenderer : IRenderer
    {
        private Memory memory = null;
        private RenderTarget renderTarget = null;
        private Texture vramTexture = null;
        private Texture paletteTexture = null;
        private VertexBuffer screenStrips = null;
        private Device device = null;
        private Effect renderersEffect = null;

        public Memory Memory
        {
            set { this.memory = value; }
        }

        public void Initialize(object data)
        {
            this.device = data as Device;

            this.renderTarget = new RenderTarget(240, 160, this.device);

            this.paletteTexture = new Texture(this.device, 256, 1, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            this.vramTexture = new Texture(this.device, 512, 256, 1, Usage.Dynamic, Format.A8, Pool.Default);

            this.screenStrips = new VertexBuffer(typeof(CustomVertex.TransformedTextured),
                160 * 4, this.device, Usage.WriteOnly, CustomVertex.TransformedTextured.Format, Pool.Default);
            this.screenStrips.Created += new EventHandler(OnScreenStripsCreated);
            this.OnScreenStripsCreated(this.screenStrips, null);

            Stream stream = typeof(ShaderRenderer).Assembly.GetManifestResourceStream("GarboDev.Graphics.Renderers.fx");
            this.renderersEffect = Effect.FromStream(this.device, stream, null, "", ShaderFlags.NotCloneable, null); 

            this.renderersEffect.SetValue("Palette", this.paletteTexture);
            this.renderersEffect.SetValue("VideoMemory", this.vramTexture);
        }

        public void Reset()
        {
            GraphicsStream stream = this.vramTexture.LockRectangle(0, LockFlags.Discard);
            stream.Write(this.memory.VideoRam);
            this.vramTexture.UnlockRectangle(0);

            stream = this.paletteTexture.LockRectangle(0, LockFlags.Discard);
            stream.Write(this.memory.PaletteRam);
            this.paletteTexture.UnlockRectangle(0);
        }

        private bool beginScene = true;
        private bool beginRender = true;
        private int lastDispcnt;
        private ushort dispCnt;

        public void RenderLine(int line)
        {
            if (this.beginRender)
            {
                this.renderTarget.BeginScene();
                this.beginRender = false;
            }

            this.dispCnt = Memory.ReadU16(this.memory.IORam, Memory.DISPCNT);

            if (this.lastDispcnt != this.dispCnt)
            {
                this.lastDispcnt = this.dispCnt;
                this.EndScene();
            }

            if (this.beginScene)
            {
                switch (this.dispCnt & 0x7)
                {
                    case 0: this.renderersEffect.Technique = "Mode0Renderer"; break;
                    case 1: this.renderersEffect.Technique = "Mode1Renderer"; break;
                    case 2: this.renderersEffect.Technique = "Mode2Renderer"; break;
                    case 3: this.renderersEffect.Technique = "Mode3Renderer"; break;

                    case 4:
                        {
                            this.renderersEffect.Technique = "Mode4Renderer";
                            int baseIdx = 0;
                            if ((this.dispCnt & (1 << 4)) == 1 << 4) baseIdx = 0xA000;
                            this.renderersEffect.SetValue("Mode4Base", baseIdx);
                        }
                        break;

                    case 5:
                        {
                            this.renderersEffect.Technique = "Mode5Renderer";
                            int baseIdx = 0;
                            if ((this.dispCnt & (1 << 4)) == 1 << 4) baseIdx += 160 * 128 * 2;
                            this.renderersEffect.SetValue("Mode5Base", baseIdx);
                        }
                        break;
                }

                int passes = this.renderersEffect.Begin(0);

                this.device.SetStreamSource(0, this.screenStrips, 0);
                this.device.VertexFormat = CustomVertex.TransformedTextured.Format;

                this.beginScene = false;
            }

            for (int i = 0; i < 1; i++)
            {
                this.renderersEffect.BeginPass(i);

                this.device.DrawPrimitives(PrimitiveType.TriangleFan, line * 4, 2);

                this.renderersEffect.EndPass();
            }

            // Hack to get it fast enough :(
            if ((line & 7) == 0)
            {
                List<uint> vramUpdate = this.memory.VramUpdated;
                if (vramUpdate.Count != 0)
                {
                    GraphicsStream stream = this.vramTexture.LockRectangle(0, LockFlags.Discard);
                    for (int i = 0; i < vramUpdate.Count; i++)
                    {
                        stream.Seek(vramUpdate[i] * Memory.VramBlockSize, SeekOrigin.Begin);
                        stream.Write(this.memory.VideoRam, (int)(vramUpdate[i] * Memory.VramBlockSize), Memory.VramBlockSize);
                    }
                    this.vramTexture.UnlockRectangle(0);
                }
            }

            List<uint> palUpdate = this.memory.PalUpdated;
            if (palUpdate.Count != 0)
            {
                GraphicsStream stream = this.paletteTexture.LockRectangle(0, LockFlags.Discard);
                for (int i = 0; i < palUpdate.Count; i++)
                {
                    if (palUpdate[i] < (512 / Memory.PalBlockSize))
                    {
                        stream.Seek(palUpdate[i] * Memory.PalBlockSize * 2, SeekOrigin.Begin);
                        for (uint j = 0; j < Memory.PalBlockSize; j += 2)
                        {
                            stream.Write(0xFF000000 | Renderer.GbaTo32(Memory.ReadU16(this.memory.PaletteRam, (palUpdate[i] * Memory.PalBlockSize) + j)));
                        }
                    }
                }
                this.paletteTexture.UnlockRectangle(0);
            }
        }

        private void EndScene()
        {
            if (!this.beginScene)
            {
                this.renderersEffect.End();
            }

            this.beginScene = true;
        }

        public object ShowFrame()
        {
            this.EndScene();

            if (!this.beginRender)
            {
                this.renderTarget.EndScene();
                this.beginRender = true;
            }

            return this.renderTarget.Texture;
        }

        private void OnScreenStripsCreated(object sender, EventArgs e)
        {
            VertexBuffer vb = (VertexBuffer)sender;

            CustomVertex.TransformedTextured[] verts = (CustomVertex.TransformedTextured[])vb.Lock(0, 0);

            int w = 240;

            for (int y = 0; y < 160; y++)
            {
                verts[(y * 4) + 0].Position = new Vector4(-0.5f, y - 0.5f, 0, 1);
                verts[(y * 4) + 0].Tu = 0;
                verts[(y * 4) + 0].Tv = y;
                verts[(y * 4) + 1].Position = new Vector4(w - 0.5f, y - 0.5f, 0, 1);
                verts[(y * 4) + 1].Tu = 240.0f;
                verts[(y * 4) + 1].Tv = y;
                verts[(y * 4) + 2].Position = new Vector4(w - 0.5f, (y + 1) - 0.5f, 0, 1);
                verts[(y * 4) + 2].Tu = 240.0f;
                verts[(y * 4) + 2].Tv = y + 1;
                verts[(y * 4) + 3].Position = new Vector4(-0.5f, (y + 1) - 0.5f, 0, 1);
                verts[(y * 4) + 3].Tu = 0;
                verts[(y * 4) + 3].Tv = y + 1;
            }

            vb.Unlock();
        }
    }
}
