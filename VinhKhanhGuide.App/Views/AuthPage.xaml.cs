using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.ViewModels;

namespace VinhKhanhGuide.App.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class AuthPage : ContentPage
{
    public AuthPage(AuthPageViewModel viewModel)
    {
        BindingContext = viewModel;

        try
        {
            InitializeComponent();
        }
        catch (XamlParseException)
        {
            BuildFallbackLayout();
        }
    }

    private void BuildFallbackLayout()
    {
        BackgroundColor = Color.FromArgb("#F4F8FF");

        var errorPanel = CreateMessagePanel(
            nameof(AuthPageViewModel.ErrorMessage),
            nameof(AuthPageViewModel.HasErrorMessage),
            "#FFF1F2",
            "#F9CBD3",
            "#BE123C");

        var activityIndicator = new ActivityIndicator
        {
            Color = Color.FromArgb("#2F80FF")
        };
        activityIndicator.SetBinding(ActivityIndicator.IsRunningProperty, nameof(AuthPageViewModel.IsBusy));
        activityIndicator.SetBinding(IsVisibleProperty, nameof(AuthPageViewModel.IsBusy));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(22, 36, 22, 28),
                Spacing = 18,
                Children =
                {
                    new Border
                    {
                        StrokeThickness = 0,
                        StrokeShape = new RoundRectangle { CornerRadius = 34 },
                        Background = new LinearGradientBrush(
                            [
                                new GradientStop(Color.FromArgb("#2F80FF"), 0.0f),
                                new GradientStop(Color.FromArgb("#5A9CFF"), 0.55f),
                                new GradientStop(Color.FromArgb("#8CC0FF"), 1.0f)
                            ],
                            new Point(0, 0),
                            new Point(1, 1)),
                        Padding = 22,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 10,
                            Children =
                            {
                                new Label
                                {
                                    Text = "VinhKhanh",
                                    FontSize = 30,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Colors.White
                                },
                                new Label
                                {
                                    FontSize = 22,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Colors.White
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.WelcomeTitle)),
                                new Label
                                {
                                    FontSize = 14,
                                    TextColor = Color.FromArgb("#EAF3FF")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.HeroDescription))
                            }
                        }
                    },
                    new Border
                    {
                        Stroke = Color.FromArgb("#D8E8FF"),
                        StrokeThickness = 1,
                        StrokeShape = new RoundRectangle { CornerRadius = 30 },
                        BackgroundColor = Colors.White,
                        Padding = 20,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 16,
                            Children =
                            {
                                new Label
                                {
                                    FontSize = 22,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.FromArgb("#102A43")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.LanguagePromptTitle)),
                                new Label
                                {
                                    FontSize = 13,
                                    TextColor = Color.FromArgb("#5E7491")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.LanguagePromptSubtitle)),
                                CreateLanguagePickerPanel(),
                                errorPanel,
                                new Button
                                {
                                    BackgroundColor = Color.FromArgb("#2F80FF"),
                                    TextColor = Colors.White,
                                    FontAttributes = FontAttributes.Bold,
                                    CornerRadius = 18,
                                    HeightRequest = 54
                                }.Bind(Button.TextProperty, nameof(AuthPageViewModel.EnterAppButtonText))
                                 .Bind(Button.CommandProperty, nameof(AuthPageViewModel.EnterAppCommand))
                                 .Bind(Button.IsEnabledProperty, nameof(AuthPageViewModel.IsNotBusy)),
                                activityIndicator
                            }
                        }
                    }
                }
            }
        };
    }

    private static View CreateMessagePanel(
        string textBinding,
        string visibleBinding,
        string backgroundColor,
        string strokeColor,
        string textColor)
    {
        var label = new Label
        {
            FontSize = 13,
            TextColor = Color.FromArgb(textColor)
        };
        label.SetBinding(Label.TextProperty, textBinding);

        var border = new Border
        {
            BackgroundColor = Color.FromArgb(backgroundColor),
            Stroke = Color.FromArgb(strokeColor),
            StrokeThickness = 1,
            Padding = new Thickness(14, 12),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = label
        };
        border.SetBinding(IsVisibleProperty, visibleBinding);
        return border;
    }

    private static View CreateLanguagePickerPanel()
    {
        var promptTitle = new Label
        {
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1B2A2F")
        };
        promptTitle.SetBinding(Label.TextProperty, nameof(AuthPageViewModel.LanguagePromptTitle));

        var promptSubtitle = new Label
        {
            FontSize = 13,
            TextColor = Color.FromArgb("#5F7075")
        };
        promptSubtitle.SetBinding(Label.TextProperty, nameof(AuthPageViewModel.LanguagePromptSubtitle));

        var picker = new Picker
        {
            Title = "Chọn ngôn ngữ",
            TitleColor = Color.FromArgb("#8AA1BF"),
            TextColor = Color.FromArgb("#102A43"),
            ItemDisplayBinding = new Binding(nameof(AudioSettingsOption.Label))
        };
        picker.SetBinding(Picker.ItemsSourceProperty, nameof(AuthPageViewModel.SupportedLanguages));
        picker.SetBinding(Picker.SelectedItemProperty, nameof(AuthPageViewModel.SelectedLanguageOption));

        var pickerBorder = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#DCEBFF"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 2),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = picker
        };

        var summary = new Label
        {
            FontSize = 12,
            TextColor = Color.FromArgb("#486581")
        };
        summary.SetBinding(Label.TextProperty, nameof(AuthPageViewModel.SelectedLanguageSummary));

        return new Border
        {
            BackgroundColor = Color.FromArgb("#F8FBFF"),
            Stroke = Color.FromArgb("#DCEBFF"),
            StrokeThickness = 1,
            Padding = 16,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            promptTitle,
                            promptSubtitle
                        }
                    },
                    pickerBorder,
                    summary
                }
            }
        };
    }
}

internal static class ViewBindingExtensions
{
    public static TView Bind<TView>(this TView view, BindableProperty property, string path)
        where TView : BindableObject
    {
        view.SetBinding(property, path);
        return view;
    }
}
