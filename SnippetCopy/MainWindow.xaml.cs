using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SnippetCopy.Models;
using System.Collections.ObjectModel; // needed for ObservableCollection

namespace SnippetCopy;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // All temporary Snippet templates
    private ObservableCollection<Snippet> snippets; // Observable Collection instead of List -> updates the UI automatically
    public MainWindow()
    {
        InitializeComponent();
        
        snippets = new ObservableCollection<Snippet>
        {
            new Snippet { Name = "README", Content = "# Projektname\n\nBeschreibung..." }, // ToDo: Create List via JSON
            new Snippet { Name = "Gitignore", Content = "bin/\nobj/\n*.user" },
        };

        snippetList.ItemsSource = snippets;
    }
    
    // Copy button
    private void Copy_Click(object sender, RoutedEventArgs e) // WPF needs sender and e
    {
        Snippet selected = snippetList.SelectedItem as Snippet;

        if (selected != null)
        {
            Clipboard.SetText(selected.Content);
        }
    }

    // Shows overview/preview of the selected snippet
    private void SnippetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Snippet selected = snippetList.SelectedItem as Snippet;

        if (selected != null)
        {
            Preview.Text = selected.Content;
        }
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        newPanel.Visibility = Visibility.Visible;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Snippet newSnippet = new Snippet
        {
            Name = newName.Text,
            Content = newContent.Text
        };
        snippets.Add(newSnippet);

        newName.Text = "";
        newContent.Text = "";
        newPanel.Visibility = Visibility.Collapsed;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        Snippet selected = snippetList.SelectedItem as Snippet;

        if (selected != null)
        {
            snippets.Remove(selected);
        }
    }
}