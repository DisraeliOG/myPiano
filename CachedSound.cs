using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace myPiano
{
    public class CachedSound
    {
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }
        
        private static readonly WaveFormat StandardFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        public CachedSound(string audioFileName)
        {
            if (!File.Exists(audioFileName))
                throw new FileNotFoundException($"Файл не найден: {audioFileName}");

            using (var audioFileReader = new AudioFileReader(audioFileName))
            {
                ISampleProvider resampler = audioFileReader;
                
                if (audioFileReader.WaveFormat.SampleRate != StandardFormat.SampleRate)
                {
                    resampler = new WdlResamplingSampleProvider(resampler, StandardFormat.SampleRate);
                }
                
                if (resampler.WaveFormat.Channels != StandardFormat.Channels)
                {
                    if (resampler.WaveFormat.Channels == 1)
                    {
                        resampler = resampler.ToStereo();
                    }
                }
                
                WaveFormat = StandardFormat;
                
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var readBuffer = new float[StandardFormat.SampleRate * StandardFormat.Channels];
                int samplesRead;
                
                while ((samplesRead = resampler.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.Take(samplesRead));
                }
                AudioData = wholeFile.ToArray();
            }
        }
    }
    
    public class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound _cachedSound;
        private long _position;
        
        private const float Volume = 0.6f; 

        public CachedSoundSampleProvider(CachedSound cachedSound)
        {
            _cachedSound = cachedSound;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = _cachedSound.AudioData.Length - _position;
            var samplesToCopy = Math.Min(availableSamples, count);
            
            if (samplesToCopy > 0)
            {
                for (int i = 0; i < samplesToCopy; i++)
                {
                    buffer[offset + i] = _cachedSound.AudioData[_position + i] * Volume;
                }
                _position += samplesToCopy;
            }
            
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat => _cachedSound.WaveFormat;
    }
}