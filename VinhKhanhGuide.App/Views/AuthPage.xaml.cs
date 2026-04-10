using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Shapes;
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
        BackgroundColor = Color.FromArgb("#F6EDE4");

        var fullNameSection = CreateInputSection("Họ và tên", nameof(AuthPageViewModel.FullName), false);
        fullNameSection.SetBinding(IsVisibleProperty, nameof(AuthPageViewModel.IsRegisterMode));

        var emailSection = CreateInputSection("Tên đăng nhập hoặc email", nameof(AuthPageViewModel.Email), false);
        var passwordSection = CreateInputSection("Mật khẩu", nameof(AuthPageViewModel.Password), true);
        var confirmPasswordSection = CreateInputSection("Xác nhận mật khẩu", nameof(AuthPageViewModel.ConfirmPassword), true);
        confirmPasswordSection.SetBinding(IsVisibleProperty, nameof(AuthPageViewModel.IsRegisterMode));

        var errorLabel = new Label
        {
            FontSize = 13,
            TextColor = Color.FromArgb("#B42318")
        };
        errorLabel.SetBinding(Label.TextProperty, nameof(AuthPageViewModel.ErrorMessage));
        errorLabel.SetBinding(IsVisibleProperty, nameof(AuthPageViewModel.HasErrorMessage));

        var activityIndicator = new ActivityIndicator
        {
            Color = Color.FromArgb("#EA580C")
        };
        activityIndicator.SetBinding(ActivityIndicator.IsRunningProperty, nameof(AuthPageViewModel.IsBusy));
        activityIndicator.SetBinding(IsVisibleProperty, nameof(AuthPageViewModel.IsBusy));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 36, 24, 28),
                Spacing = 20,
                Children =
                {
                    new Border
                    {
                        StrokeThickness = 0,
                        StrokeShape = new RoundRectangle { CornerRadius = 30 },
                        Background = new LinearGradientBrush(
                            [
                                new GradientStop(Color.FromArgb("#7C2D12"), 0.0f),
                                new GradientStop(Color.FromArgb("#C2410C"), 0.55f),
                                new GradientStop(Color.FromArgb("#FB923C"), 1.0f)
                            ],
                            new Point(0, 0),
                            new Point(1, 1)),
                        Padding = 24,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 12,
                            Children =
                            {
                                new Label
                                {
                                    Text = "VinhKhanh Guide",
                                    FontSize = 30,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Colors.White
                                },
                                new Label
                                {
                                    Text = "Đăng nhập trước khi vào app để dùng bản đồ, GPS và thuyết minh.",
                                    FontSize = 14,
                                    TextColor = Color.FromArgb("#FFEDD5")
                                }
                            }
                        }
                    },
                    new Border
                    {
                        StrokeThickness = 0,
                        StrokeShape = new RoundRectangle { CornerRadius = 28 },
                        BackgroundColor = Colors.White,
                        Padding = 22,
                        Content = new VerticalStackLayout
                        {
                            Spacing = 16,
                            Children =
                            {
                                new Label
                                {
                                    FontSize = 24,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.FromArgb("#1F2937")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.FormTitle)),
                                new Label
                                {
                                    FontSize = 14,
                                    TextColor = Color.FromArgb("#6B7280")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.FormSubtitle)),
                                new Grid
                                {
                                    ColumnDefinitions = new ColumnDefinitionCollection
                                    {
                                        new ColumnDefinition(GridLength.Star),
                                        new ColumnDefinition(GridLength.Star)
                                    },
                                    ColumnSpacing = 10,
                                    Children =
                                    {
                                        CreateActionButton("Đăng nhập", nameof(AuthPageViewModel.SwitchToLoginCommand), 0),
                                        CreateActionButton("Đăng ký", nameof(AuthPageViewModel.SwitchToRegisterCommand), 1)
                                    }
                                },
                                fullNameSection,
                                emailSection,
                                passwordSection,
                                confirmPasswordSection,
                                errorLabel,
                                new Button
                                {
                                    BackgroundColor = Color.FromArgb("#EA580C"),
                                    TextColor = Colors.White,
                                    FontAttributes = FontAttributes.Bold,
                                    CornerRadius = 20,
                                    Padding = new Thickness(18, 14)
                                }.Bind(Button.TextProperty, nameof(AuthPageViewModel.SubmitButtonText))
                                 .Bind(Button.CommandProperty, nameof(AuthPageViewModel.SubmitCommand))
                                 .Bind(Button.IsEnabledProperty, nameof(AuthPageViewModel.IsNotBusy)),
                                activityIndicator,
                                new Label
                                {
                                    FontSize = 12,
                                    TextColor = Color.FromArgb("#6B7280")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.FormHint))
                            }
                        }
                    }
                }
            }
        };
    }

    private static View CreateInputSection(string labelText, string bindingPath, bool isPassword)
    {
        var entry = new Entry
        {
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#111827"),
            PlaceholderColor = Color.FromArgb("#9CA3AF"),
            IsPassword = isPassword,
            ClearButtonVisibility = isPassword ? ClearButtonVisibility.Never : ClearButtonVisibility.WhileEditing
        };
        entry.SetBinding(Entry.TextProperty, bindingPath);

        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = labelText,
                    FontSize = 13,
                    TextColor = Color.FromArgb("#374151")
                },
                new Border
                {
                    BackgroundColor = Color.FromArgb("#FFF8F1"),
                    StrokeThickness = 0,
                    Padding = new Thickness(14, 8),
                    StrokeShape = new RoundRectangle { CornerRadius = 18 },
                    Content = entry
                }
            }
        };
    }

    private static Button CreateActionButton(string text, string commandPath, int column)
    {
        var button = new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb("#F4E7DA"),
            TextColor = Color.FromArgb("#7C2D12"),
            CornerRadius = 18,
            Padding = new Thickness(14, 10)
        };
        button.SetBinding(Button.CommandProperty, commandPath);
        Grid.SetColumn(button, column);
        return button;
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
