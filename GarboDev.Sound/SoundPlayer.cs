using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.DirectX.DirectSound;

namespace GarboDev.Sound
{
    public delegate void PullAudio(short[] buffer, int length);

    public class SoundPlayer : IDisposable
    {
        private Device _soundDevice;
        private SecondaryBuffer _soundBuffer;
        private readonly int _samplesPerUpdate;
        private readonly AutoResetEvent[] _fillEvent = new AutoResetEvent[2];
        private readonly Thread _thread;
        private readonly PullAudio _pullAudio;
        private readonly short _channels;
        private bool _halted;
        private bool _running;

        public SoundPlayer(Control owner, PullAudio pullAudio, short channels)
        {
            _channels = channels;
            _pullAudio = pullAudio;

            _soundDevice = new Device();
            _soundDevice.SetCooperativeLevel(owner, CooperativeLevel.Priority);

            // Set up our wave format to 44,100Hz, with 16 bit resolution
            var wf = new WaveFormat
            {
                FormatTag = WaveFormatTag.Pcm, SamplesPerSecond = 44100, BitsPerSample = 16, Channels = channels
            };
            wf.BlockAlign = (short)(wf.Channels * wf.BitsPerSample / 8);
            wf.AverageBytesPerSecond = wf.SamplesPerSecond * wf.BlockAlign;

            _samplesPerUpdate = 512;

            // Create a buffer with 2 seconds of sample data
            var bufferDesc = new BufferDescription(wf)
            {
                BufferBytes = _samplesPerUpdate * wf.BlockAlign * 2,
                ControlPositionNotify = true,
                GlobalFocus = true
            };

            _soundBuffer = new SecondaryBuffer(bufferDesc, _soundDevice);

            var notify = new Notify(_soundBuffer);

            _fillEvent[0] = new AutoResetEvent(false);
            _fillEvent[1] = new AutoResetEvent(false);

            // Set up two notification events, one at halfway, and one at the end of the buffer
            var posNotify = new BufferPositionNotify[2];
            posNotify[0] = new BufferPositionNotify
            {
                Offset = bufferDesc.BufferBytes / 2 - 1,
                EventNotifyHandle = _fillEvent[0].SafeWaitHandle.DangerousGetHandle()
            };
            posNotify[1] = new BufferPositionNotify
            {
                Offset = bufferDesc.BufferBytes - 1,
                EventNotifyHandle = _fillEvent[1].SafeWaitHandle.DangerousGetHandle()
            };

            notify.SetNotificationPositions(posNotify);

            _thread = new Thread(SoundPlayback) {Priority = ThreadPriority.Highest};

            Pause();
            _running = true;

            _thread.Start();
        }

        public void Pause()
        {
            if (_halted) return;

            _halted = true;
            _soundBuffer.Stop();

            Monitor.Enter(_thread);
        }

        public void Resume()
        {
            if (!_halted) return;

            _halted = false;
            _soundBuffer.Play(0, BufferPlayFlags.Looping);

            Monitor.Pulse(_thread);
            Monitor.Exit(_thread);
        }

        private void SoundPlayback()
        {
            lock (_thread)
            {
                if (!_running) return;

                // Set up the initial sound buffer to be the full length
                var bufferLength = _samplesPerUpdate * 2 * _channels;
                var soundData = new short[bufferLength];

                // Prime it with the first x seconds of data
                _pullAudio(soundData, soundData.Length);
                _soundBuffer.Write(0, soundData, LockFlag.None);

                // Start it playing
                _soundBuffer.Play(0, BufferPlayFlags.Looping);

                var lastWritten = 0;
                while (_running)
                {
                    if (_halted)
                    {
                        Monitor.Pulse(_thread);
                        Monitor.Wait(_thread);
                    }

                    // Wait on one of the notification events with a 3ms timeout
                    WaitHandle.WaitAny(_fillEvent, 3, true);

                    // Get the current play position (divide by two because we are using 16 bit samples)
                    if (_soundBuffer != null)
                    {
                        var tmp = _soundBuffer.PlayPosition / 2;

                        // Generate new sounds from lastWritten to tmp in the sound buffer
                        if (tmp == lastWritten)
                        {
                            continue;
                        }
                        else
                        {
                            soundData = new short[(tmp - lastWritten + bufferLength) % bufferLength];
                        }

                        _pullAudio(soundData, soundData.Length);

                        // Write in the generated data
                        _soundBuffer.Write(lastWritten * 2, soundData, LockFlag.None);

                        // Save the position we were at
                        lastWritten = tmp;
                    }
                }
            }
        }

        public void Dispose()
        {
            _running = false;
            Resume();

            if (_soundBuffer != null)
            {
                _soundBuffer.Dispose();
                _soundBuffer = null;
            }
            if (_soundDevice != null)
            {
                _soundDevice.Dispose();
                _soundDevice = null;
            }
        }
    }
}