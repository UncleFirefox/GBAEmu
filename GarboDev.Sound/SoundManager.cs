using System.Collections.Generic;
using GarboDev.CrossCutting;

namespace GarboDev.Sound
{
    public class SoundManager
    {
        private readonly Memory _memory;
        private readonly Queue<byte>[] _soundQueue = new Queue<byte>[2];
        private byte _latchedA, _latchedB;
        private int _frequency, _cyclesPerSample;
        private int _leftover;

        private readonly short[] _soundBuffer = new short[40000];
        private int _soundBufferPos;
        private int _lastSoundBufferPos;

        public SoundManager(Memory memory, int frequency)
        {
            Frequency = frequency;

            _memory = memory;
            _memory.IncrementSoundFifoA = IncrementFifoA;
            _memory.IncrementSoundFifoB = IncrementFifoB;
            _memory.ResetSoundFifoA = ResetFifoA;
            _memory.ResetSoundFifoB = ResetFifoB;

            _soundQueue[0] = new Queue<byte>(32);
            _soundQueue[1] = new Queue<byte>(32);
        }

        #region Public Properties
        public int Frequency
        {
            get => _frequency;
            set
            {
                _frequency = value;
                _cyclesPerSample = (Constants.CpuFreq << 5) / _frequency;
            }
        }

        public int QueueSizeA => _soundQueue[0].Count;

        public int QueueSizeB => _soundQueue[1].Count;

        public int SamplesMixed
        {
            get
            {
                var value = _soundBufferPos - _lastSoundBufferPos;
                if (value < 0) value += _soundBuffer.Length;
                return value;
            }
        }
        #endregion

        #region Public Methods
        public void GetSamples(short[] buffer, int length)
        {
            for (var i = 0; i < length; i++)
            {
                if (_lastSoundBufferPos == _soundBuffer.Length)
                {
                    _lastSoundBufferPos = 0;
                }
                buffer[i] = _soundBuffer[_lastSoundBufferPos++];
            }
        }

        public void Mix(int cycles)
        {
            var soundCntH = Memory.ReadU16(_memory.IORam, Memory.SOUNDCNT_H);
            var soundCntX = Memory.ReadU16(_memory.IORam, Memory.SOUNDCNT_X);

            cycles <<= 5;
            cycles += _leftover;

            if (cycles > 0)
            {
                // Precompute loop invariants
                var directA = (short)(sbyte)(_latchedA);
                var directB = (short)(sbyte)(_latchedB);

                if ((soundCntH & (1 << 2)) == 0)
                {
                    directA >>= 1;
                }
                if ((soundCntH & (1 << 3)) == 0)
                {
                    directB >>= 1;
                }

                while (cycles > 0)
                {
                    short l = 0, r = 0;

                    cycles -= _cyclesPerSample;

                    // Mixing
                    if ((soundCntX & (1 << 7)) != 0)
                    {
                        if ((soundCntH & (1 << 8)) != 0)
                        {
                            r += directA;
                        }
                        if ((soundCntH & (1 << 9)) != 0)
                        {
                            l += directA;
                        }
                        if ((soundCntH & (1 << 12)) != 0)
                        {
                            r += directB;
                        }
                        if ((soundCntH & (1 << 13)) != 0)
                        {
                            l += directB;
                        }
                    }

                    if (_soundBufferPos == _soundBuffer.Length)
                    {
                        _soundBufferPos = 0;
                    }

                    _soundBuffer[_soundBufferPos++] = (short)(l << 6);
                    _soundBuffer[_soundBufferPos++] = (short)(r << 6);
                }
            }

            _leftover = cycles;
        }

        public void ResetFifoA()
        {
            _soundQueue[0].Clear();
            _latchedA = 0;
        }

        public void ResetFifoB()
        {
            _soundQueue[1].Clear();
            _latchedB = 0;
        }

        public void IncrementFifoA()
        {
            for (var i = 0; i < 4; i++)
            {
                EnqueueDSoundSample(0, _memory.IORam[Memory.FIFO_A_L + i]);
            }
        }

        public void IncrementFifoB()
        {
            for (var i = 0; i < 4; i++)
            {
                EnqueueDSoundSample(1, _memory.IORam[Memory.FIFO_B_L + i]); 
            }
        }

        public void DequeueA()
        {
            if (_soundQueue[0].Count > 0)
            {
                _latchedA = _soundQueue[0].Dequeue();
            }
        }

        public void DequeueB()
        {
            if (_soundQueue[1].Count > 0)
            {
                _latchedB = _soundQueue[1].Dequeue();
            }
        }
        #endregion Public Methods

        private void EnqueueDSoundSample(int channel, byte sample)
        {
            if (_soundQueue[channel].Count < 32)
            {
                _soundQueue[channel].Enqueue(sample);
            }
        }
    }
}
