using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GarboDev.Graphics;
using GarboDev.Sound;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GarboDev.WinForms
{
    partial class MainWindow : Form
    {
        private DisassemblyWindow _disassembly;
        private PaletteWindow _palette;
        private SpriteWindow _sprites;

        private GbaManager.GbaManager _gbaManager;
        private readonly SoundPlayer _soundPlayer;

        private Device _device;

        private VertexBuffer _screenQuad;
        private Texture _backgroundTexture;

        private enum RendererType
        {
            GdiRenderer,
            D3DRenderer,
            ShaderRenderer
        }

        private RendererType _rendererType;

        private int _width, _height;

        private string _biosFilename;

        public MainWindow()
        {
            InitializeComponent();

            _gbaManager = new GbaManager.GbaManager();

            // Initialize the sound subsystem
            if (EnableSound.Checked)
            {
                _soundPlayer = new SoundPlayer(this, _gbaManager.AudioMixer, 2);
                _soundPlayer.Resume();
            }

            // Initialize the video subsystem
            GetRenderTypeFromOptions();
            SetRenderType(_rendererType);

            _width = 240 * 2;
            _height = 160 * 2;
            ClientSize = new Size(_width, _height + MainMenu.Height + StatusStrip.Height);

            var timer = new Timer {Interval = 50};
            timer.Tick += UpdateFps;
            timer.Enabled = true;

            _biosFilename = "C:\\Documents and Settings\\Administrator\\My Documents\\Visual Studio\\Projects\\GarboDev\\gbabios.bin";

            if (OptionsUseBios.Checked)
            {
                LoadBios();
            }

            OptionsSkipBios.Checked = _gbaManager.SkipBios;
            OptionsLimitFps.Checked = _gbaManager.LimitFps;
        }

        private void LoadBios()
        {
            try
            {
                using (Stream stream = new FileStream(_biosFilename, FileMode.Open))
                {
                    var rom = new byte[(int)stream.Length];
                    stream.Read(rom, 0, (int)stream.Length);

                    _gbaManager.LoadBios(rom);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("Unable to load bios file, disabling bios (irq's will not work)\n" + exception.Message, "Error");

                OptionsBiosFile.Checked = false;
            }
        }

        private void UpdateFps(object sender, EventArgs e)
        {
            if (_gbaManager == null) return;

            var t1 = _gbaManager.FramesRendered;
            var t2 = _gbaManager.SecondsSinceStarted;

            _framesRendered.Enqueue(t1);
            _secondsSinceStarted.Enqueue(t2);

            var frameDiff = t1 - _framesRendered.Peek();
            var timeDiff = t2 - _secondsSinceStarted.Peek();

            if (_framesRendered.Count > 10)
            {
                _framesRendered.Dequeue();
                _secondsSinceStarted.Dequeue();
            }

            StatusStrip.Items[0].Text = $"Fps: {frameDiff / timeDiff,2:f}";
        }

        protected override void OnResizeBegin(EventArgs e)
        {
            Halt();
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            if (!FilePause.Checked)
            {
                Resume();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            _width = ClientSize.Width;
            _height = ClientSize.Height - MainMenu.Height - StatusStrip.Height;

            if (_screenQuad != null)
            {
                OnScreenQuadCreated(_screenQuad, null);
            }
        }

        public void Shutdown()
        {
            _gbaManager.Close();
            _gbaManager = null;

            _soundPlayer?.Dispose();
            _disassembly?.Close();
            _palette?.Close();
            _sprites?.Close();

            _device.Dispose();
        }

        private void InitializeD3D()
        {
            try
            {
                var presentParams = new PresentParameters {Windowed = true, SwapEffect = SwapEffect.Copy};

                if (!OptionsVsync.Checked)
                {
                    presentParams.PresentationInterval = PresentInterval.Immediate;
                }

                _device = new Device(0, DeviceType.Hardware, this, CreateFlags.HardwareVertexProcessing,
                    presentParams);
            }
            catch (DirectXException exception)
            {
                MessageBox.Show("Unable to create Direct3D instance (perhaps you need to install the managed directx client?)\n" + exception.Message);
                throw exception;
            }

            _screenQuad = new VertexBuffer(typeof(CustomVertex.TransformedColoredTextured),
                4, _device, Usage.WriteOnly, CustomVertex.TransformedColoredTextured.Format, Pool.Default);
            _screenQuad.Created += OnScreenQuadCreated;
            OnScreenQuadCreated(_screenQuad, null);

            if (_rendererType == RendererType.D3DRenderer)
            {
                _backgroundTexture = new Texture(_device, 240, 160, 1, Usage.None,
                    Format.X8R8G8B8, Pool.Managed);
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys keys);

        public void CheckKeysHit()
        {
            var keymap = new Keys[]
                {
                    Keys.A,
                    Keys.B,
                    Keys.RShiftKey,
                    Keys.Enter,
                    Keys.NumPad6,
                    Keys.NumPad4,
                    Keys.NumPad8,
                    Keys.NumPad2,
                    Keys.R,
                    Keys.L
                };

            ushort keyreg = 0x3FF;

            for (var i = 0; i < keymap.Length; i++)
            {
                if (GetAsyncKeyState(keymap[i]) < 0)
                {
                    keyreg &= (ushort)(~(1U << i));
                }
                else
                {
                    keyreg |= (ushort)(1U << i);
                }
            }

            _gbaManager.KeyState = keyreg;
        }

        private void GetRenderTypeFromOptions()
        {
            if (OptionsRenderersD3D.Checked)
            {
                _rendererType = RendererType.D3DRenderer;
            }
            else if (OptionsRenderersShader.Checked)
            {
                _rendererType = RendererType.ShaderRenderer;
            }
            else if (OptionsRenderersGDI.Checked)
            {
                _rendererType = RendererType.GdiRenderer;
            }
        }

        private void SetRenderType(RendererType rendererType)
        {
            OptionsRenderersD3D.Checked = false;
            OptionsRenderersShader.Checked = false;
            OptionsRenderersGDI.Checked = false;

            var wasHalted = _gbaManager.Halted;
            _gbaManager.Halt();

            if (_device != null)
            {
                _device.Dispose();
            }

            _rendererType = rendererType;

            switch (_rendererType)
            {
                case RendererType.D3DRenderer:
                    OptionsRenderersD3D.Checked = true;

                    InitializeD3D();
                    _gbaManager.VideoManager.Presenter = RenderD3D;

                    var renderer = new Renderer();
                    renderer.Initialize(null);

                    _gbaManager.VideoManager.Renderer = renderer;
                    break;

                case RendererType.GdiRenderer:
                    OptionsRenderersGDI.Checked = true;

                    new Bitmap(240, 160, PixelFormat.Format32bppRgb);
                    break;

                case RendererType.ShaderRenderer:
                    OptionsRenderersShader.Checked = true;

                    InitializeD3D();
                    _gbaManager.VideoManager.Presenter = RenderShader;
                    _gbaManager.Memory.EnableVramUpdating();

                    var shaderRenderer = new ShaderRenderer();
                    shaderRenderer.Initialize(_device);

                    _gbaManager.VideoManager.Renderer = shaderRenderer;
                    break;
            }

            if (!wasHalted)
            {
                _gbaManager.Resume();
            }
        }

        private void RenderGdi(object data)
        {
            Invalidate();
        }

        private void RenderShader(object data)
        {
            if (_device == null)
                return;

            _device.Clear(ClearFlags.Target, Color.Black, 0.0f, 0);

            _device.BeginScene();

            _device.SetTexture(0, data as Texture);
            _device.TextureState[0].ColorOperation = TextureOperation.Modulate;
            _device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
            _device.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;
            _device.TextureState[0].AlphaOperation = TextureOperation.Disable;

            _device.RenderState.CullMode = Cull.None;
            _device.RenderState.Lighting = false;
            _device.RenderState.ZBufferEnable = false;

            _device.SetStreamSource(0, _screenQuad, 0);
            _device.VertexFormat = CustomVertex.TransformedColoredTextured.Format;
            _device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);

            _device.SetStreamSource(0, null, 0);

            _device.EndScene();
            _device.Present();
        }

        private void RenderD3D(object data)
        {
            if (_device == null)
                return;

            var surface = _backgroundTexture.GetSurfaceLevel(0);
            var stream = surface.LockRectangle(LockFlags.None);
            if (data is uint[] videoBuffer)
            {
                stream.Write(videoBuffer);
            }
            surface.UnlockRectangle();

            _device.Clear(ClearFlags.Target, Color.White, 0.0f, 0);
            _device.BeginScene();

            _device.SetTexture(0, _backgroundTexture);
            _device.TextureState[0].ColorOperation = TextureOperation.Modulate;
            _device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
            _device.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;
            _device.TextureState[0].AlphaOperation = TextureOperation.Disable;

            _device.RenderState.CullMode = Cull.None;
            _device.RenderState.Lighting = false;
            _device.RenderState.ZBufferEnable = false;

            _device.SetStreamSource(0, _screenQuad, 0);
            _device.VertexFormat = CustomVertex.TransformedColoredTextured.Format;
            _device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);

            _device.SetStreamSource(0, null, 0);

            _device.EndScene();
            _device.Present();
        }

        private readonly Queue<int> _framesRendered = new Queue<int>();
        private readonly Queue<double> _secondsSinceStarted = new Queue<double>();

        private void FileOpen_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();

            dialog.Filter = "bin files (*.bin;*.gba)|*.bin;*.gba|All files (*.*)|*.*";
            dialog.FilterIndex = 0;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                using (var stream = dialog.OpenFile())
                {
                    var romSize = 1;
                    while (romSize < stream.Length)
                    {
                        romSize <<= 1;
                    }

                    var rom = new byte[romSize];
                    stream.Read(rom, 0, (int)stream.Length);

                    _gbaManager.LoadRom(rom);
                }

                if (!FilePause.Checked)
                {
                    Resume();
                }
            }
        }

        private void FileExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_gbaManager != null)
            {
                e.Cancel = true;

                Shutdown();

                Close();
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            if (!FilePause.Checked)
            {
                Resume();
            }
        }

        protected override void OnLostFocus(EventArgs e)
        {
            if (_gbaManager != null)
            {
                Halt();
            }
        }

        private void DebugDissassembly_Click(object sender, EventArgs e)
        {
            _disassembly = new DisassemblyWindow(_gbaManager);
            _disassembly.Show();

            _disassembly.Location = new Point(Location.X + Width + 15, Location.Y);
        }

        private void DebugPalette_Click(object sender, EventArgs e)
        {
            _palette = new PaletteWindow(_gbaManager);
            _palette.Show();
        }

        private void DebugSprites_Click(object sender, EventArgs e)
        {
            _sprites = new SpriteWindow(_gbaManager);
            _sprites.Show();
        }

        private void OnScreenQuadCreated(object sender, EventArgs e)
        {
            var vb = (VertexBuffer)sender;
            var verts = (CustomVertex.TransformedColoredTextured[])vb.Lock(0, 0);

            float topx = 0, topy = MainMenu.Height;

            verts[0].Position = new Vector4(topx - 0.5f, topy - 0.5f, 0, 1);
            verts[0].Color = Color.White.ToArgb();
            verts[0].Tu = 0; verts[0].Tv = 0;
            verts[1].Position = new Vector4(topx + _width - 0.5f, topy - 0.5f, 0, 1);
            verts[1].Color = Color.White.ToArgb();
            verts[1].Tu = 1; verts[1].Tv = 0;
            verts[2].Position = new Vector4(topx + _width - 0.5f, topy + _height - 0.5f, 0, 1);
            verts[2].Color = Color.White.ToArgb();
            verts[2].Tu = 1; verts[2].Tv = 1;
            verts[3].Position = new Vector4(topx - 0.5f, topy + _height - 0.5f, 0, 1);
            verts[3].Color = Color.White.ToArgb();
            verts[3].Tu = 0; verts[3].Tv = 1;

            vb.Unlock();
        }

        private void FilePause_Click(object sender, EventArgs e)
        {
            if (!FilePause.Checked)
            {
                Resume();
            }
            else
            {
                Halt();
                _device.Present();
            }
        }

        private void FileReset_Click(object sender, EventArgs e)
        {
            _soundPlayer.Pause();
            _gbaManager.Reset();

            if (!FilePause.Checked)
            {
                Resume();
            }
        }

        private void OptionsBiosFile_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "bin files (*.bin;*.gba)|*.bin;*.gba|All files (*.*)|*.*",
                FilterIndex = 0,
                FileName = _biosFilename
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _biosFilename = dialog.FileName;
                LoadBios();
            }
        }

        private void OptionsSkipBios_Click(object sender, EventArgs e)
        {
            _gbaManager.SkipBios = OptionsSkipBios.Checked;
        }

        private void OptionsVsync_Click(object sender, EventArgs e)
        {
            if (_device != null)
            {
                var parameters = _device.PresentationParameters;

                if (OptionsVsync.Checked)
                {
                    parameters.PresentationInterval = PresentInterval.Default;
                }
                else
                {
                    parameters.PresentationInterval = PresentInterval.Immediate;
                }

                Halt();
                _device.Reset(parameters);
                Resume();
            }
        }

        private void OptionsLimitFps_Click(object sender, EventArgs e)
        {
            _gbaManager.LimitFps = OptionsLimitFps.Checked;
        }

        private void OptionsRenderersD3D_Click(object sender, EventArgs e)
        {
            SetRenderType(RendererType.D3DRenderer);
        }

        private void OptionsRenderersShader_Click(object sender, EventArgs e)
        {
            SetRenderType(RendererType.ShaderRenderer);
        }

        private void OptionsRenderersGDI_Click(object sender, EventArgs e)
        {
            SetRenderType(RendererType.GdiRenderer);
        }

        private void OptionsSizex1_Click(object sender, EventArgs e)
        {
            ResizeWindow(1, 1);
        }

        private void OptionsSizex2_Click(object sender, EventArgs e)
        {
            ResizeWindow(2, 2);
        }

        private void OptionsSizex3_Click(object sender, EventArgs e)
        {
            ResizeWindow(3, 3);
        }

        private void ResizeWindow(int xScale, int yScale)
        {
            _width = 240 * xScale;
            _height = 160 * yScale;

            Halt();

            ClientSize = new Size(_width, _height + MainMenu.Height + StatusStrip.Height);

            if (!FilePause.Checked)
            {
                Resume();
            }
        }

        private void Halt()
        {
            _soundPlayer.Pause();
            _gbaManager.Halt();
        }

        private void Resume()
        {
            _gbaManager.Resume();
            if (EnableSound.Checked)
            {
                _soundPlayer.Resume();
            }
        }

        private void enableSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableSound.Checked = !EnableSound.Checked;
            if (!EnableSound.Checked)
            {
                _soundPlayer.Pause();
            }
            else
            {
                _soundPlayer.Resume();
            }
        }
    }
}