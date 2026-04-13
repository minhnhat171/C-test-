using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using VinhKhanhGuide.App.ViewModels;

namespace VinhKhanhGuide.App.Views;

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
        BackgroundColor = Color.FromArgb("#F4EFE7");

        var errorPanel = CreateMessagePanel(
            nameof(AuthPageViewModel.ErrorMessage),
            nameof(AuthPageViewModel.HasErrorMessage),
            "#FDEEEE",
            "#E4B6B0",
            "#A53A32");

        var activityIndicator = new ActivityIndicator
        {
            Color = Color.FromArgb("#215C57")
        };
        activityIndicator.SetBinding(ActivityIndicator.IsRunningProperty, nameof(AuthPageViewModel.IsBusy));
        activityIndicator.SetBinding(IsVisibleProperty, nameof(AuthPageViewModel.IsBusy));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(22, 32, 22, 26),
                Spacing = 18,
                Children =
                {
                    new Border
                    {
                        StrokeThickness = 0,
                        StrokeShape = new RoundRectangle { CornerRadius = 34 },
                        Background = new LinearGradientBrush(
                            [
                                new GradientStop(Color.FromArgb("#173A43"), 0.0f),
                                new GradientStop(Color.FromArgb("#215C57"), 0.55f),
                                new GradientStop(Color.FromArgb("#3C7E6C"), 1.0f)
                            ],
                            new Point(0, 0),
                            new Point(1, 1)),
                        Padding = 22,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 12,
                            Children =
                            {
                                new Label
                                {
                                    Text = "VinhKhanh",
                                    FontSize = 30,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.FromArgb("#FFF9F2")
                                },
                                new Label
                                {
                                    Text = "Bản hiện tại dành cho du khách vào nhanh để trải nghiệm ngay giao diện chính, chọn box quán và nghe thuyết minh.",
                                    FontSize = 15,
                                    TextColor = Color.FromArgb("#E9F3EF")
                                },
                                new Border
                                {
                                    BackgroundColor = Color.FromArgb("#F5EBDD"),
                                    StrokeThickness = 0,
                                    Padding = new Thickness(14, 10),
                                    StrokeShape = new RoundRectangle { CornerRadius = 18 },
                                    Content = new Label
                                    {
                                        Text = "Luồng quét QR là hướng đúng và mình đã chừa chỗ để nối ở prompt sau.",
                                        FontSize = 13,
                                        TextColor = Color.FromArgb("#1C4748")
                                    }
                                }
                            }
                        }
                    },
                    new Border
                    {
                        Stroke = Color.FromArgb("#E4D8CC"),
                        StrokeThickness = 1,
                        StrokeShape = new RoundRectangle { CornerRadius = 30 },
                        BackgroundColor = Color.FromArgb("#FFFDFC"),
                        Padding = 20,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 16,
                            Children =
                            {
                                new Label
                                {
                                    FontSize = 24,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.FromArgb("#1B2A2F")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.WelcomeTitle)),
                                new Label
                                {
                                    FontSize = 13,
                                    TextColor = Color.FromArgb("#67757A")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.WelcomeSubtitle)),
                                new Grid
                                {
                                    ColumnDefinitions = new ColumnDefinitionCollection
                                    {
                                        new ColumnDefinition(GridLength.Star),
                                        new ColumnDefinition(GridLength.Star)
                                    },
                                    RowDefinitions = new RowDefinitionCollection
                                    {
                                        new RowDefinition(GridLength.Auto),
                                        new RowDefinition(GridLength.Auto)
                                    },
                                    ColumnSpacing = 12,
                                    RowSpacing = 12,
                                    Children =
                                    {
                                        CreateHeroStepCard("B1", "Mở app", 0, 0),
                                        CreateHeroStepCard("B2", "Bấm vào nhanh", 0, 1),
                                        CreateHeroStepCard("B3", "Chọn box để nghe", 1, 0, columnSpan: 2, widthRequest: 172)
                                    }
                                },
                                errorPanel,
                                new Button
                                {
                                    BackgroundColor = Color.FromArgb("#C6672F"),
                                    TextColor = Colors.White,
                                    FontAttributes = FontAttributes.Bold,
                                    CornerRadius = 18,
                                    HeightRequest = 54
                                }.Bind(Button.TextProperty, nameof(AuthPageViewModel.EnterAppButtonText))
                                 .Bind(Button.CommandProperty, nameof(AuthPageViewModel.EnterAppCommand))
                                 .Bind(Button.IsEnabledProperty, nameof(AuthPageViewModel.IsNotBusy)),
                                activityIndicator,
                                new Label
                                {
                                    FontSize = 12,
                                    TextColor = Color.FromArgb("#6C7A7F")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.WelcomeHint))
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

    private static Border CreateHeroStepCard(
        string stepLabel,
        string title,
        int row,
        int column,
        int columnSpan = 1,
        double widthRequest = -1)
    {
        var border = new Border
        {
            Padding = new Thickness(16, 14),
            BackgroundColor = Color.FromArgb("#F5EBDD"),
            Stroke = Color.FromArgb("#E4D5C4"),
            StrokeThickness = 1,
            HeightRequest = 88,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label
                    {
                        Text = stepLabel,
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#A96A45")
                    },
                    new Label
                    {
                        Text = title,
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#1C4748")
                    }
                }
            }
        };

        if (widthRequest > 0)
        {
            border.WidthRequest = widthRequest;
            border.HorizontalOptions = LayoutOptions.Center;
        }

        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        Grid.SetColumnSpan(border, columnSpan);
        return border;
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
