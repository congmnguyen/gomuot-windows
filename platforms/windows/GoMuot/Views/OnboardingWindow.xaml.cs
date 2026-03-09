using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using GoMuot.Services;

namespace GoMuot.Views;

/// <summary>
/// Onboarding window for first-time setup
/// </summary>
public partial class OnboardingWindow : Window
{
    private readonly SettingsService _settings;
    private readonly Ellipse[] _dots;

    public OnboardingWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        _dots = new[] { Dot1, Dot2, Dot3 };
        UpdateDots();
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (PageTabs.SelectedIndex < PageTabs.Items.Count - 1)
        {
            PageTabs.SelectedIndex++;
            UpdateDots();
        }
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (PageTabs.SelectedIndex > 0)
        {
            PageTabs.SelectedIndex--;
            UpdateDots();
        }
    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        _settings.Save();
        Close();
    }

    private void UpdateDots()
    {
        var activeBrush = (SolidColorBrush)FindResource("PrimaryBrush");
        var inactiveBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)); // #E5E7EB

        for (int i = 0; i < _dots.Length; i++)
        {
            _dots[i].Fill = i == PageTabs.SelectedIndex ? activeBrush : inactiveBrush;
        }
    }
}
