using System.Windows;

namespace ImageCompressor
{
    public class PdfExportSettings
    {
        public string PageSize { get; set; } = "A4";
        public bool IsLandscape { get; set; } = false;
    }

    public partial class PdfSettingsDialog : Window
    {
        public PdfExportSettings Settings { get; private set; }

        public PdfSettingsDialog()
        {
            InitializeComponent();
            Settings = new PdfExportSettings();
            PageSizeComboBox.SelectedIndex = 0;
        }

        private void InitializeComponent()
        {
            Title = "PDF导出设置";
            Width = 350;
            Height = 230;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new System.Windows.Controls.Grid();
            grid.Margin = new Thickness(16);

            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            var pageSizeLabel = new System.Windows.Controls.TextBlock
            {
                Text = "页面大小:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            System.Windows.Controls.Grid.SetRow(pageSizeLabel, 0);
            grid.Children.Add(pageSizeLabel);

            PageSizeComboBox = new System.Windows.Controls.ComboBox
            {
                Height = 32,
                Margin = new Thickness(0, 8, 0, 8),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            PageSizeComboBox.Items.Add("A4");
            PageSizeComboBox.Items.Add("Letter");
            PageSizeComboBox.Items.Add("A3");
            PageSizeComboBox.SelectedIndex = 0;
            System.Windows.Controls.Grid.SetRow(PageSizeComboBox, 1);
            grid.Children.Add(PageSizeComboBox);

            var orientationPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 0)
            };
            PortraitRadio = new System.Windows.Controls.RadioButton
            {
                Content = "纵向",
                IsChecked = true,
                Margin = new Thickness(0, 0, 16, 0)
            };
            LandscapeRadio = new System.Windows.Controls.RadioButton
            {
                Content = "横向"
            };
            orientationPanel.Children.Add(PortraitRadio);
            orientationPanel.Children.Add(LandscapeRadio);
            System.Windows.Controls.Grid.SetRow(orientationPanel, 2);
            grid.Children.Add(orientationPanel);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            
            var okButton = new System.Windows.Controls.Button
            {
                Content = "确定",
                Width = 80,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };
            okButton.Click += OkButton_Click;
            
            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "取消",
                Width = 80,
                Height = 32
            };
            cancelButton.Click += (s, e) => DialogResult = false;
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            System.Windows.Controls.Grid.SetRow(buttonPanel, 3);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }

        private System.Windows.Controls.ComboBox PageSizeComboBox = null!;
        private System.Windows.Controls.RadioButton PortraitRadio = null!;
        private System.Windows.Controls.RadioButton LandscapeRadio = null!;

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.PageSize = PageSizeComboBox.SelectedItem?.ToString() ?? "A4";
            Settings.IsLandscape = LandscapeRadio.IsChecked == true;
            DialogResult = true;
        }
    }
}
