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

        var fullNameSection = CreateInputSection(
            "Họ và tên",
            nameof(AuthPageViewModel.FullName),
            "Nguyễn Văn A",
            isPassword: false);
        fullNameSection.SetBinding(IsVisibleProperty, nameof(AuthPageViewModel.IsRegisterMode));

        var usernameSection = CreateInputSection(
            "Tên đăng nhập",
            nameof(AuthPageViewModel.Username),
            "Ví dụ: user",
            isPassword: false);
        var passwordSection = CreateInputSection(
            "Mật khẩu",
            nameof(AuthPageViewModel.Password),
            "Tối thiểu 8 ký tự",
            isPassword: true);
        var confirmPasswordSection = CreateInputSection(
            "Xác nhận mật khẩu",
            nameof(AuthPageViewModel.ConfirmPassword),
            "Nhập lại mật khẩu",
            isPassword: true);
        confirmPasswordSection.SetBinding(IsVisibleProperty, nameof(AuthPageViewModel.IsRegisterMode));

        var accountTypeSection = CreatePickerSection();
        accountTypeSection.SetBinding(IsVisibleProperty, nameof(AuthPageViewModel.IsRegisterMode));

        var errorPanel = CreateMessagePanel(
            nameof(AuthPageViewModel.ErrorMessage),
            nameof(AuthPageViewModel.HasErrorMessage),
            "#FDEEEE",
            "#E4B6B0",
            "#A53A32");

        var successPanel = CreateMessagePanel(
            nameof(AuthPageViewModel.SuccessMessage),
            nameof(AuthPageViewModel.HasSuccessMessage),
            "#EDF7F0",
            "#B6D6BF",
            "#246446");

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
                Padding = new Thickness(22, 30, 22, 26),
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
                            Spacing = 20,
                            Children =
                            {
                                new Label
                                {
                                    Text = "VinhKhanh",
                                    FontSize = 28,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.FromArgb("#FFF9F2")
                                },
                                new Label
                                {
                                    Text = "Đăng nhập trước khi vào bản đồ để bật GPS, theo dõi hành trình và nghe thuyết minh tự động.",
                                    FontSize = 15,
                                    TextColor = Color.FromArgb("#E9F3EF")
                                },
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
                                        CreateHeroStepCard("B1", "Xác thực", 0, 0),
                                        CreateHeroStepCard("B2", "Mở bản đồ", 0, 1),
                                        CreateHeroStepCard("B3", "GPS và audio", 1, 0, columnSpan: 2, widthRequest: 148)
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
                                    FontSize = 22,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.FromArgb("#1B2A2F")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.FormTitle)),
                                new Label
                                {
                                    FontSize = 13,
                                    TextColor = Color.FromArgb("#67757A")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.FormSubtitle)),
                                new Border
                                {
                                    BackgroundColor = Color.FromArgb("#F1E7D9"),
                                    StrokeThickness = 0,
                                    Padding = new Thickness(5),
                                    StrokeShape = new RoundRectangle { CornerRadius = 21 },
                                    Content = new Grid
                                    {
                                        ColumnDefinitions = new ColumnDefinitionCollection
                                        {
                                            new ColumnDefinition(GridLength.Star),
                                            new ColumnDefinition(GridLength.Star)
                                        },
                                        ColumnSpacing = 8,
                                        Children =
                                        {
                                            CreateActionButton("Đăng nhập", nameof(AuthPageViewModel.SwitchToLoginCommand), 0),
                                            CreateActionButton("Đăng ký", nameof(AuthPageViewModel.SwitchToRegisterCommand), 1)
                                        }
                                    }
                                },
                                fullNameSection,
                                usernameSection,
                                passwordSection,
                                confirmPasswordSection,
                                accountTypeSection,
                                errorPanel,
                                successPanel,
                                new Button
                                {
                                    BackgroundColor = Color.FromArgb("#C6672F"),
                                    TextColor = Colors.White,
                                    FontAttributes = FontAttributes.Bold,
                                    CornerRadius = 18,
                                    HeightRequest = 52
                                }.Bind(Button.TextProperty, nameof(AuthPageViewModel.SubmitButtonText))
                                 .Bind(Button.CommandProperty, nameof(AuthPageViewModel.SubmitCommand))
                                 .Bind(Button.IsEnabledProperty, nameof(AuthPageViewModel.IsNotBusy)),
                                activityIndicator,
                                new Label
                                {
                                    FontSize = 12,
                                    TextColor = Color.FromArgb("#6C7A7F")
                                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.FormHint))
                            }
                        }
                    }
                }
            }
        };
    }

    private static View CreateInputSection(string labelText, string bindingPath, string placeholder, bool isPassword)
    {
        var entry = new Entry
        {
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#13242A"),
            Placeholder = placeholder,
            PlaceholderColor = Color.FromArgb("#9BA8AD"),
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
                    TextColor = Color.FromArgb("#34474E")
                },
                new Border
                {
                    BackgroundColor = Color.FromArgb("#FAF6F0"),
                    Stroke = Color.FromArgb("#D9CEC1"),
                    StrokeThickness = 1,
                    Padding = new Thickness(14, 10),
                    StrokeShape = new RoundRectangle { CornerRadius = 18 },
                    Content = entry
                }
            }
        };
    }

    private static View CreatePickerSection()
    {
        var picker = new Picker
        {
            Title = "Chọn loại khách",
            TextColor = Color.FromArgb("#13242A"),
            TitleColor = Color.FromArgb("#9BA8AD")
        };
        picker.SetBinding(Picker.ItemsSourceProperty, nameof(AuthPageViewModel.AccountTypeOptions));
        picker.SetBinding(Picker.SelectedItemProperty, nameof(AuthPageViewModel.SelectedAccountType));

        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = "Loại khách",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#34474E")
                },
                new Border
                {
                    BackgroundColor = Color.FromArgb("#FAF6F0"),
                    Stroke = Color.FromArgb("#D9CEC1"),
                    StrokeThickness = 1,
                    Padding = new Thickness(14, 4),
                    StrokeShape = new RoundRectangle { CornerRadius = 18 },
                    Content = picker
                },
                new Label
                {
                    FontSize = 12,
                    TextColor = Color.FromArgb("#6C7A7F")
                }.Bind(Label.TextProperty, nameof(AuthPageViewModel.SelectedAccountTypeHint))
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

    private static Button CreateActionButton(string text, string commandPath, int column)
    {
        var button = new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb("#F1E7D9"),
            TextColor = Color.FromArgb("#7A4D2D"),
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 17,
            HeightRequest = 46
        };
        button.SetBinding(Button.CommandProperty, commandPath);
        Grid.SetColumn(button, column);
        return button;
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
