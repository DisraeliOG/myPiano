using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace myPiano;

public partial class MainWindow : Window
{
    private const string AppVersion = "1.0";
    private const string AppAuthor = "Усанков Леонид";

    private readonly HashSet<Key> _pressedKeys = new HashSet<Key>();
    private readonly Brush _normalInputBackground = Brushes.White;
    private bool _audioReady;

    public MainWindow()
    {
        InitializeComponent();

        _audioReady = AudioManager.Initialize();
        if (!_audioReady)
        {
            SetStatus(AppStatus.Error, AudioManager.LastErrorMessage);
            MessageBox.Show(
                AudioManager.LastErrorMessage,
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        else
        {
            SetStatus(AppStatus.Ready);
        }

        Closed += (s, e) => AudioManager.Dispose();
    }

    private bool PlayNote(string note)
    {
        if (!_audioReady)
        {
            SetStatus(AppStatus.Error, AudioManager.LastErrorMessage);
            return false;
        }

        if (AudioManager.Play(note))
            return true;

        SetStatus(AppStatus.Error, AudioManager.LastErrorMessage);
        return false;
    }

    private void Note_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (!PlayNote(button.Name))
            return;

        AnimatePress(button);

        Task.Delay(150).ContinueWith(_ =>
        {
            Dispatcher.Invoke(() => AnimateRelease(button));
        });
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (SplashScreen.Visibility == Visibility.Visible || e.IsRepeat || _pressedKeys.Contains(e.Key))
            return;

        _pressedKeys.Add(e.Key);

        string? noteToPlay = GetNoteFromKey(e.Key);

        if (noteToPlay == null)
            return;

        if (!PlayNote(noteToPlay))
            return;

        if (FindName(noteToPlay) is Button button)
            AnimatePress(button);
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (_pressedKeys.Contains(e.Key))
            _pressedKeys.Remove(e.Key);

        string? noteToRelease = GetNoteFromKey(e.Key);

        if (noteToRelease != null && FindName(noteToRelease) is Button btn)
            AnimateRelease(btn);
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
        string resourceName = IsBlackKey(btn) ? "PressBlackAnimation" : "PressAnimation";
        var animation = TryFindResource(resourceName) as ColorAnimation;
        if (animation == null || btn.Background is not SolidColorBrush brush)
            return;

        btn.Background = new SolidColorBrush(brush.Color);
        btn.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
    }

    private void AnimateRelease(Button btn)
    {
        string resourceName = IsBlackKey(btn) ? "ReleaseBlackAnimation" : "ReleaseAnimation";
        var animation = TryFindResource(resourceName) as ColorAnimation;
        if (animation == null || btn.Background is not SolidColorBrush brush)
            return;

        btn.Background = new SolidColorBrush(brush.Color);
        btn.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
    }

    private static bool IsBlackKey(Button btn)
    {
        return btn.Name.Contains("b") || btn.Content?.ToString()?.Contains("#") == true;
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

        foreach (var note in notes)
        {
            if (!PlayNote(note))
                return;
        }

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
            if (FindName(note) is Button btn)
                AnimatePress(btn);
        }

        await Task.Delay(200);

        foreach (var note in notes)
        {
            if (FindName(note) is Button btn)
                AnimateRelease(btn);
        }
    }

    private async void PlayMelody_Click(object sender, RoutedEventArgs e)
    {
        string text = MelodyInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            ShowInputError();
            SetStatus(AppStatus.Error, "Введите хотя бы одну ноту");
            return;
        }

        ClearInputError();
        SetStatus(AppStatus.Playing);

        string[] tokens = text.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            if (token == "|" || token == "-" || token == "_" || token.All(c => c == ':'))
            {
                await Task.Delay(400);
                continue;
            }

            int durationFactor = token.Count(c => c == ':');
            string noteName = token.Replace(":", "");
            int duration = 250 + durationFactor * 200;

            if (!PlayNoteSafely(noteName))
            {
                return;
            }

            HighlightSingleKey(noteName, duration);
            await Task.Delay(duration);
        }

        SetStatus(AppStatus.Success);
    }

    private bool PlayNoteSafely(string note)
    {
        try
        {
            return PlayNote(note);
        }
        catch
        {
            SetStatus(AppStatus.Error);
            return false;
        }
    }

    private async void HighlightSingleKey(string note, int duration)
    {
        if (FindName(note) is not Button btn)
            return;

        AnimatePress(btn);
        await Task.Delay(duration);
        AnimateRelease(btn);
    }

    private void MelodyInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(MelodyInput.Text))
            ClearInputError();
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        SplashScreen.Visibility = Visibility.Collapsed;
        MelodyInput.Focus();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            $"Пианино\nВерсия: {AppVersion}\nРазработчик: {AppAuthor}\n\nИспользуемые технологии:\nC#\nWPF\nNAudio",
            "О программе",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        MelodyInput.Clear();
        ClearInputError();
        SetStatus(AppStatus.Ready);
        MelodyInput.Focus();
    }

    private void ShowInputError()
    {
        MelodyInput.Background = new SolidColorBrush(Color.FromRgb(255, 226, 226));
        InputErrorText.Visibility = Visibility.Visible;
        MelodyInput.Focus();
    }

    private void ClearInputError()
    {
        MelodyInput.Background = _normalInputBackground;
        InputErrorText.Visibility = Visibility.Collapsed;
    }

    private void SetStatus(AppStatus status, string? customMessage = null)
    {
        string message = customMessage ?? status switch
        {
            AppStatus.Ready => "Программа готова к работе",
            AppStatus.Playing => "Выполняется воспроизведение мелодии",
            AppStatus.Success => "Мелодия успешно воспроизведена",
            AppStatus.Error => "Ошибка воспроизведения",
            _ => "Программа готова к работе"
        };

        Brush color = status switch
        {
            AppStatus.Ready => Brushes.Gray,
            AppStatus.Playing => Brushes.DodgerBlue,
            AppStatus.Success => Brushes.ForestGreen,
            AppStatus.Error => Brushes.Red,
            _ => Brushes.Gray
        };

        StatusText.Text = message;
        StatusText.Foreground = color;
        StatusIndicator.Fill = color;
    }
}

public enum AppStatus
{
    Ready,
    Playing,
    Success,
    Error
}
