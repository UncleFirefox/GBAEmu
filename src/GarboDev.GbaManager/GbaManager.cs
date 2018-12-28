//#define ARM_DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GarboDev.Cores;
using GarboDev.CrossCutting;
using GarboDev.Graphics;
using GarboDev.Sound;

namespace GarboDev.GbaManager
{
    public class GbaManager
    {
        private readonly Thread _executionThread = null;

        private bool _running;

        public delegate void CpuUpdateDelegate(Arm7Processor processor, Memory memory);
        private event CpuUpdateDelegate onCpuUpdate = null;

        public Arm7Processor Arm7 { get; private set; } = null;

        public VideoManager VideoManager { get; private set; } = null;

        public SoundManager SoundManager { get; private set; } = null;

        public Memory Memory { get; private set; } = null;

        public Dictionary<uint, bool> Breakpoints => this.Arm7.Breakpoints;

        public ushort KeyState
        {
            get
            {
                if (this.Memory != null)
                {
                    return this.Memory.KeyState;
                }

                return 0x3FF;
            }

            set => this.Arm7.KeyState = value;
        }

        public int FramesRendered { get; set; }

        public event CpuUpdateDelegate OnCpuUpdate
        {
            add
            {
                this.onCpuUpdate += value;
                this.onCpuUpdate(this.Arm7, this.Memory);
            }
            remove => this.onCpuUpdate -= value;
        }

        public bool SkipBios { get; set; } = true;

        public bool LimitFps { get; set; } = true;

        public bool Halted { get; private set; }

        public GbaManager()
        {
            this.FramesRendered = 0;

            this.Halt();

            this._running = true;

            this._executionThread = new Thread(new ParameterizedThreadStart(RunEmulationLoop));
            this._executionThread.Start();

            // Wait for the initialization to complete
            Monitor.Wait(this);
        }

        public void Close()
        {
            if (this._running)
            {
                this._running = false;

                if (this.Halted)
                {
                    this.Resume();
                }

                Monitor.Enter(this);
                Monitor.Exit(this);
            }
        }

        public void Halt()
        {
            if (this.Halted) return;

            this.Halted = true;
            Monitor.Enter(this);
        }

        public void Resume()
        {
            if (!this.Halted) return;

            this.Halted = false;

            this.iterations = 0;
            this.timer.Start();

            Monitor.Pulse(this);
            Monitor.Exit(this);
        }

        public void Reset()
        {
            this.Halt();

            this.Arm7.Reset(this.SkipBios);
            this.Memory.Reset();
            this.VideoManager.Reset();
        }

        public PullAudio AudioMixer => this.AudioMixerStereo;

        public void AudioMixerStereo(short[] buffer, int length)
        {
            // even = left, odd = right
            if (this.SoundManager.SamplesMixed > Math.Max(500, length))
            {
                this.SoundManager.GetSamples(buffer, length);
            }
        }

        public void LoadState(BinaryReader state)
        {
        }

        public void SaveState(BinaryWriter state)
        {
            state.Write("GARB");
        }

        public void LoadBios(byte[] biosRom)
        {
            this.Memory.LoadBios(biosRom);

            this.onCpuUpdate?.Invoke(this.Arm7, this.Memory);
        }

        public void LoadRom(byte[] cartRom)
        {
            this.Halt();

            byte[] logo = new byte[]
                    {
            			0x24,0xff,0xae,0x51,0x69,0x9a,0xa2,0x21,
            			0x3d,0x84,0x82,0x0a,0x84,0xe4,0x09,0xad,
            			0x11,0x24,0x8b,0x98,0xc0,0x81,0x7f,0x21,
            			0xa3,0x52,0xbe,0x19,0x93,0x09,0xce,0x20,
	    	        	0x10,0x46,0x4a,0x4a,0xf8,0x27,0x31,0xec,
        	    		0x58,0xc7,0xe8,0x33,0x82,0xe3,0xce,0xbf,
	        	    	0x85,0xf4,0xdf,0x94,0xce,0x4b,0x09,0xc1,
		        	    0x94,0x56,0x8a,0xc0,0x13,0x72,0xa7,0xfc,
    		    	    0x9f,0x84,0x4d,0x73,0xa3,0xca,0x9a,0x61,
        		    	0x58,0x97,0xa3,0x27,0xfc,0x03,0x98,0x76,
	        		    0x23,0x1d,0xc7,0x61,0x03,0x04,0xae,0x56,
    		        	0xbf,0x38,0x84,0x00,0x40,0xa7,0x0e,0xfd,
	    		        0xff,0x52,0xfe,0x03,0x6f,0x95,0x30,0xf1,
            			0x97,0xfb,0xc0,0x85,0x60,0xd6,0x80,0x25,
	            		0xa9,0x63,0xbe,0x03,0x01,0x4e,0x38,0xe2,
		        	    0xf9,0xa2,0x34,0xff,0xbb,0x3e,0x03,0x44,
			            0x78,0x00,0x90,0xcb,0x88,0x11,0x3a,0x94,
            			0x65,0xc0,0x7c,0x63,0x87,0xf0,0x3c,0xaf,
	            		0xd6,0x25,0xe4,0x8b,0x38,0x0a,0xac,0x72,
		            	0x21,0xd4,0xf8,0x07
                    };

            Array.Copy(logo, 0, cartRom, 4, logo.Length);
            cartRom[0xB2] = 0x96;
            cartRom[0xBD] = 0;
            for (int i = 0xA0; i <= 0xBC; i++) cartRom[0xBD] = (byte)(cartRom[0xBD] - cartRom[i]);
            cartRom[0xBD] = (byte)((cartRom[0xBD] - 0x19) & 0xFF);

            this.Memory.LoadCartridge(cartRom);

            this.Reset();

            onCpuUpdate?.Invoke(this.Arm7, this.Memory);
        }

        public void Step()
        {
            this.Halt();

            this.Arm7.Step();

            onCpuUpdate?.Invoke(this.Arm7, this.Memory);
        }

        public void StepScanline()
        {
            this.Halt();

            this.Arm7.Execute(960);
            this.VideoManager.RenderLine();
            this.VideoManager.EnterHBlank(this.Arm7);
            this.Arm7.Execute(272);
            this.VideoManager.LeaveHBlank(this.Arm7);

            onCpuUpdate?.Invoke(this.Arm7, this.Memory);
        }

        private HighPerformanceTimer timer = new HighPerformanceTimer();
        private double iterations;

        public double SecondsSinceStarted => this.timer.ElapsedSeconds;

        private void RunEmulationLoop(object threadParams)
        {
            this.Memory = new Memory();
            this.SoundManager = new SoundManager(this.Memory, 44100);
            this.Arm7 = new Arm7Processor(this.Memory, this.SoundManager);
            this.VideoManager = new VideoManager(() => FramesRendered++) {Memory = this.Memory};

            lock (this)
            {
                Monitor.Pulse(this);
                Monitor.Wait(this);

                this.iterations = 0;
                this.timer.Start();

                int vramCycles = 0;
                bool inHblank = false;

                HighPerformanceTimer profileTimer = new HighPerformanceTimer();

                while (this._running)
                {
                    if (this.Halted)
                    {
                        Monitor.Pulse(this);
                        Monitor.Wait(this);
                    }

                    if (!this.LimitFps ||
                        iterations < timer.ElapsedSeconds)
                    {
                        const int numSteps = 2284;
                        const int cycleStep = 123;

                        for (int i = 0; i < numSteps; i++)
                        {
                            if (vramCycles <= 0)
                            {
                                if (!this._running || this.Halted) break;

                                if (inHblank)
                                {
                                    vramCycles += 960;
                                    this.VideoManager.LeaveHBlank(this.Arm7);
                                    inHblank = false;
                                }
                                else
                                {
                                    vramCycles += 272;
                                    this.VideoManager.RenderLine();
                                    this.VideoManager.EnterHBlank(this.Arm7);
                                    inHblank = true;
                                }
                            }

                            this.Arm7.Execute(cycleStep);

#if ARM_DEBUG
                            if (this.arm7.BreakpointHit)
                            {
                                this.waitingToHalt = true;
                                Monitor.Wait(this);
                            }
#endif

                            vramCycles -= cycleStep;

                            this.Arm7.FireIrq();
                        }

                        iterations += (cycleStep * numSteps) / ((double)Constants.CpuFreq);
                    }

                    Thread.Sleep(0);
                }

                Monitor.Pulse(this);
            }
        }
    }
}