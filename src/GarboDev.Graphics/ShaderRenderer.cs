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
        private Memory _memory;
        private RenderTarget _renderTarget;
        private Texture _vramTexture;
        private Texture _paletteTexture;
        private VertexBuffer _screenStrips;
        private Device _device;
        private Effect _renderersEffect;

        public Memory Memory
        {
            set => _memory = value;
        }

        public void Initialize(object data)
        {
            _device = data as Device;

            _renderTarget = new RenderTarget(240, 160, _device);

            _paletteTexture = new Texture(_device, 256, 1, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            _vramTexture = new Texture(_device, 512, 256, 1, Usage.Dynamic, Format.A8, Pool.Default);

            _screenStrips = new VertexBuffer(typeof(CustomVertex.TransformedTextured),
                160 * 4, _device, Usage.WriteOnly, CustomVertex.TransformedTextured.Format, Pool.Default);
            _screenStrips.Created += OnScreenStripsCreated;
            OnScreenStripsCreated(_screenStrips, null);

            var stream = typeof(ShaderRenderer).Assembly.GetManifestResourceStream("GarboDev.Graphics.Renderers.fx");
            _renderersEffect = Effect.FromStream(_device, stream, null, "", ShaderFlags.NotCloneable, null); 

            _renderersEffect.SetValue("Palette", _paletteTexture);
            _renderersEffect.SetValue("VideoMemory", _vramTexture);
        }

        public void Reset()
        {
            var stream = _vramTexture.LockRectangle(0, LockFlags.Discard);
            stream.Write(_memory.VideoRam);
            _vramTexture.UnlockRectangle(0);

            stream = _paletteTexture.LockRectangle(0, LockFlags.Discard);
            stream.Write(_memory.PaletteRam);
            _paletteTexture.UnlockRectangle(0);
        }

        private bool _beginScene = true;
        private bool _beginRender = true;
        private int _lastDispcnt;
        private ushort _dispCnt;

        public void RenderLine(int line)
        {
            if (_beginRender)
            {
                _renderTarget.BeginScene();
                _beginRender = false;
            }

            _dispCnt = Memory.ReadU16(_memory.IORam, Memory.DISPCNT);

            if (_lastDispcnt != _dispCnt)
            {
                _lastDispcnt = _dispCnt;
                EndScene();
            }

            if (_beginScene)
            {
                switch (_dispCnt & 0x7)
                {
                    case 0: _renderersEffect.Technique = "Mode0Renderer"; break;
                    case 1: _renderersEffect.Technique = "Mode1Renderer"; break;
                    case 2: _renderersEffect.Technique = "Mode2Renderer"; break;
                    case 3: _renderersEffect.Technique = "Mode3Renderer"; break;

                    case 4:
                        {
                            _renderersEffect.Technique = "Mode4Renderer";
                            var baseIdx = 0;
                            if ((_dispCnt & (1 << 4)) == 1 << 4) baseIdx = 0xA000;
                            _renderersEffect.SetValue("Mode4Base", baseIdx);
                        }
                        break;

                    case 5:
                        {
                            _renderersEffect.Technique = "Mode5Renderer";
                            var baseIdx = 0;
                            if ((_dispCnt & (1 << 4)) == 1 << 4) baseIdx += 160 * 128 * 2;
                            _renderersEffect.SetValue("Mode5Base", baseIdx);
                        }
                        break;
                }

                var passes = _renderersEffect.Begin(0);

                _device.SetStreamSource(0, _screenStrips, 0);
                _device.VertexFormat = CustomVertex.TransformedTextured.Format;

                _beginScene = false;
            }

            for (var i = 0; i < 1; i++)
            {
                _renderersEffect.BeginPass(i);

                _device.DrawPrimitives(PrimitiveType.TriangleFan, line * 4, 2);

                _renderersEffect.EndPass();
            }

            // Hack to get it fast enough :(
            if ((line & 7) == 0)
            {
                var vramUpdate = _memory.VramUpdated;
                if (vramUpdate.Count != 0)
                {
                    var stream = _vramTexture.LockRectangle(0, LockFlags.Discard);
                    for (var i = 0; i < vramUpdate.Count; i++)
                    {
                        stream.Seek(vramUpdate[i] * Memory.VramBlockSize, SeekOrigin.Begin);
                        stream.Write(_memory.VideoRam, (int)(vramUpdate[i] * Memory.VramBlockSize), Memory.VramBlockSize);
                    }
                    _vramTexture.UnlockRectangle(0);
                }
            }

            var palUpdate = _memory.PalUpdated;
            if (palUpdate.Count != 0)
            {
                var stream = _paletteTexture.LockRectangle(0, LockFlags.Discard);
                for (var i = 0; i < palUpdate.Count; i++)
                {
                    if (palUpdate[i] < (512 / Memory.PalBlockSize))
                    {
                        stream.Seek(palUpdate[i] * Memory.PalBlockSize * 2, SeekOrigin.Begin);
                        for (uint j = 0; j < Memory.PalBlockSize; j += 2)
                        {
                            stream.Write(0xFF000000 | Renderer.GbaTo32(Memory.ReadU16(_memory.PaletteRam, (palUpdate[i] * Memory.PalBlockSize) + j)));
                        }
                    }
                }
                _paletteTexture.UnlockRectangle(0);
            }
        }

        private void EndScene()
        {
            if (!_beginScene)
            {
                _renderersEffect.End();
            }

            _beginScene = true;
        }

        public object ShowFrame()
        {
            EndScene();

            if (!_beginRender)
            {
                _renderTarget.EndScene();
                _beginRender = true;
            }

            return _renderTarget.Texture;
        }

        private void OnScreenStripsCreated(object sender, EventArgs e)
        {
            var vb = (VertexBuffer)sender;

            var verts = (CustomVertex.TransformedTextured[])vb.Lock(0, 0);

            var w = 240;

            for (var y = 0; y < 160; y++)
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
