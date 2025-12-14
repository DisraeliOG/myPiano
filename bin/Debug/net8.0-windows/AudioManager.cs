using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;

namespace myPiano
{
    public static class AudioManager
    {
        private static IWavePlayer _outputDevice;
        private static MixingSampleProvider _mixer;
        private static Dictionary<string, CachedSound> _soundCache = new Dictionary<string, CachedSound>();
        
        private static readonly Dictionary<string, string> FilePaths = new Dictionary<string, string>
        {
            { "C4", "Audio/C4.mp3" },
            { "Db4", "Audio/Db4.mp3" },
            { "D4", "Audio/D4.wav" },
            { "Eb4", "Audio/Eb4.mp3" },
            { "E4", "Audio/E4.wav" },
            { "F4", "Audio/F4.wav" },
            { "Gb4", "Audio/Gb4.mp3" },
            { "G4", "Audio/G4.wav" },
            { "Ab4", "Audio/Ab4.mp3" },
            { "A4", "Audio/A4.wav" },
            { "Bb4", "Audio/Bb4.mp3" },
            { "B4", "Audio/B4.wav" },
            { "C5", "Audio/C5.mp3" },
            { "Db5", "Audio/Db5.mp3" },
            { "D5", "Audio/D5.mp3" },
            { "Eb5", "Audio/Eb5.mp3" },
            { "E5", "Audio/E5.mp3" },
            { "F5", "Audio/F5.mp3" },
            { "Gb5", "Audio/Gb5.mp3" },
            { "G5", "Audio/G5.mp3" },
            { "Ab5", "Audio/Ab5.mp3" },
            { "A5", "Audio/A5.mp3" },
            { "Bb5", "Audio/Bb5.mp3" },
            { "B5", "Audio/B5.mp3" }
        };

        public static void Initialize()
        {
            try 
            {
                _outputDevice = new WaveOutEvent { DesiredLatency = 200 }; 
                
                _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
                _mixer.ReadFully = true; 
                
                _outputDevice.Init(_mixer);
                _outputDevice.Play();
                
                foreach (var kvp in FilePaths)
                {
                    if (File.Exists(kvp.Value))
                    {
                        try
                        {
                            _soundCache[kvp.Key] = new CachedSound(kvp.Value);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Не удалось загрузить {kvp.Key}: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Файл не найден: {kvp.Value}");
                    }
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Критическая ошибка аудио: {ex.Message}");
            }
        }

        public static void Play(string note)
        {
            if (!_soundCache.ContainsKey(note)) return;
            
            _mixer.AddMixerInput(new CachedSoundSampleProvider(_soundCache[note]));
        }

        public static void Dispose()
        {
            _outputDevice.Dispose();
        }
    }
}