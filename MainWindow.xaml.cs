using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace myPiano;

public partial class MainWindow : Window
{
    private readonly HashSet<Key> _pressedKeys = new HashSet<Key>();

    public MainWindow()
    {
        InitializeComponent();
        AudioManager.Initialize();
        this.Closed += (s, e) => AudioManager.Dispose();
    }

    private void PlayNote(string note)
    {
        AudioManager.Play(note);
    }
    
    private void Note_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            PlayNote(button.Name);
            AnimatePress(button);
            
            Task.Delay(150).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() => AnimateRelease(button));
            });
        }
    }
    
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.IsRepeat || _pressedKeys.Contains(e.Key))
            return;
        
        _pressedKeys.Add(e.Key);
        
        string? noteToPlay = GetNoteFromKey(e.Key);

        if (noteToPlay != null)
        {
            PlayNote(noteToPlay);

            var button = this.FindName(noteToPlay) as Button;
            if (button != null)
            {
                AnimatePress(button);
            }
        }
    }
    
    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (_pressedKeys.Contains(e.Key))
            _pressedKeys.Remove(e.Key);

        string? noteToRelease = GetNoteFromKey(e.Key);

        if (noteToRelease != null)
        {
            var btn = FindName(noteToRelease) as Button;
            if (btn != null)
                AnimateRelease(btn);
        }
    }

    private string? GetNoteFromKey(Key key)
    {
        return key switch
        {
            Key.Z => "C4",
            Key.S => "Db4",
            Key.X => "D4",
            Key.D => "Eb4",
            Key.C => "E4",
            Key.V => "F4",
            Key.G => "Gb4",
            Key.B => "G4",
            Key.H => "Ab4",
            Key.N => "A4",
            Key.J => "Bb4",
            Key.M => "B4",
            Key.Q => "C5",
            Key.F1 => "Db5",
            Key.W => "D5",
            Key.F2 => "Eb5",
            Key.E => "E5",
            Key.R => "F5",
            Key.F4 => "Gb5",
            Key.T => "G5",
            Key.F5 => "Ab5",
            Key.Y => "A5",
            Key.F6 => "Bb5",
            Key.U => "B5",
            _ => null
        };
    }

    private void AnimatePress(Button btn)
    {
        var animation = TryFindResource("PressAnimation") as ColorAnimation;
        if (animation == null || !(btn.Background is SolidColorBrush brush)) return;
        
        btn.Background = new SolidColorBrush(brush.Color);
        btn.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
    }

    private void AnimateRelease(Button btn)
    {
        bool isBlackKey = (btn.Background as SolidColorBrush)?.Color == Colors.Black || 
                          btn.Name?.Contains("b") == true || 
                          btn.Content?.ToString()?.Contains("#") == true;

        string resourceName = isBlackKey ? "ReleaseBlackAnimation" : "ReleaseAnimation";
        
        var animation = TryFindResource(resourceName) as ColorAnimation;
        if (animation == null || !(btn.Background is SolidColorBrush brush)) return;

        btn.Background = new SolidColorBrush(brush.Color);
        btn.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
    }
    
    private void Chord_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;

        string? chord = btn.Content?.ToString();
        
        List<string>? notes = chord switch
        {
            "C" => new() { "C4", "E4", "G4" },
            "Dm" => new() { "D4", "F4", "A4" },
            "Em" => new() { "E4", "G4", "B4" },
            "F" => new() { "F4", "A4", "C5" },
            "G" => new() { "G4", "B4", "D5" },
            "Am" => new() { "A4", "C5", "E5" },
            "Bdim" => new() { "B4", "D5", "F5" },
            _ => null
        };

        if (notes == null)
            return;
        
        foreach (var n in notes)
            PlayNote(n);
        HighlightKeys(notes);
        AnimateChordButton(btn);
    }

    
    private async void AnimateChordButton(Button btn)
    {
        var original = btn.Background;
        btn.Background = new SolidColorBrush(Color.FromRgb(255, 230, 150));
        await Task.Delay(150);
        btn.Background = original;
    }
    
    private async void HighlightKeys(List<string> notes)
    {
        foreach (var note in notes)
        {
            var btn = this.FindName(note) as Button;
            if (btn != null)
            {
                AnimatePress(btn);
            }
        }
        
        await Task.Delay(200);

        foreach (var note in notes)
        {
            var btn = this.FindName(note) as Button;
            if (btn != null)
            {
                AnimateRelease(btn);
            }
        }
    }
    
    private async void PlayMelody_Click(object sender, RoutedEventArgs e)
    {
        string text = MelodyInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(text))
            return;
        
        string[] tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            if (token == "|" || token == "-" || token == "_")
            {
                await Task.Delay(400);
                continue;
            }
            
            int durationFactor = token.Count(c => c == ':');
            string noteName = token.Replace(":", "");

            int duration = 250 + durationFactor * 200;
            
            PlayNoteSafely(noteName);
            HighlightSingleKey(noteName, duration);
            await Task.Delay(duration);
        }
    }
    
    private void PlayNoteSafely(string note)
    {
        try
        {
            AudioManager.Play(note);
        }
        catch { }
    }
    private async void HighlightSingleKey(string note, int duration)
    {
        var btn = FindName(note) as Button;
        if (btn == null)
            return;

        AnimatePress(btn);
        await Task.Delay(duration);
        AnimateRelease(btn);
    }
}