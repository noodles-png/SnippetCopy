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
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json; // needed for ObservableCollection

namespace SnippetCopy;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // All temporary Snippet templates
    private ObservableCollection<Snippet> snippets; // Observable Collection instead of List -> updates the UI automatically
    
    private string savePath = "snippets.json";
    
    public MainWindow()
    {
        InitializeComponent();

        if (File.Exists(savePath))
        {
            LoadSnippets();
        }
        else
        {
            snippets = new ObservableCollection<Snippet>();
            SaveSnippets();
        }
        
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
        SaveSnippets();
    }
    
    private Snippet editSnippet;

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        Snippet selected = snippetList.SelectedItem as Snippet;

        if (selected != null)
        {
            editSnippet = selected;
            newName.Text = selected.Name;
            newContent.Text = selected.Content;
            newPanel.Visibility = Visibility.Visible;
        }
    }
    
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        Snippet selected = snippetList.SelectedItem as Snippet;

        if (selected != null)
        {
            snippets.Remove(selected);
            SaveSnippets();
        }
    }
    
    // Adds new snippet to JSON database
    private void SaveSnippets()
    {
        string json = JsonSerializer.Serialize(snippets);
        File.WriteAllText(savePath, json);
    }
    
    // Loads existing snippet
    private void LoadSnippets()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            var loaded =  JsonSerializer.Deserialize<ObservableCollection<Snippet>>(json);
            if (loaded != null)
            {
                snippets = loaded;
            }
        }
    }
}