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
        editSnippet = null;
        newName.Text = "";
        newContent.Text = "";
        newCategory.SelectedIndex = -1;
        Preview.Visibility = Visibility.Collapsed;
        newPanel.Visibility = Visibility.Visible;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (editSnippet != null)
        {
            editSnippet.Name = newName.Text;
            editSnippet.Content = newContent.Text;
            editSnippet.Category = newCategory.SelectedItem as string;
            editSnippet = null;
        }
        else
        {
            Snippet newSnippet = new Snippet
                    {
                        Name = newName.Text,
                        Content = newContent.Text,
                        Category = newCategory.SelectedItem as string
                    };
                    snippets.Add(newSnippet);
        }
        

        newName.Text = "";
        newContent.Text = "";
        newCategory.SelectedIndex = -1;
        Preview.Visibility = Visibility.Visible;
        newPanel.Visibility = Visibility.Collapsed;
        SaveSnippets();
        UpdateCategories();
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
            newCategory.SelectedItem = selected.Category;
            Preview.Visibility = Visibility.Collapsed;
            newPanel.Visibility = Visibility.Visible;
        }
    }
    
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        Snippet selected = snippetList.SelectedItem as Snippet;
        
        if (selected != null)
        {
            var result = MessageBox.Show("Do you really want to delete?", "Delete", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                snippets.Remove(selected);
                Preview.Visibility = Visibility.Collapsed;
                SaveSnippets();
                UpdateCategories(); 
            }
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
    
    // Segments the snippets into categories
    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        string category = filterCategory.SelectedItem as string;

        if (category == "All")
        {
            snippetList.ItemsSource = snippets;
        }
        else
        {
            snippetList.ItemsSource = snippets.Where(s => s.Category == category).ToList();
        }
    }
    
    private void UpdateCategories()
    {
        var categories = snippets.Select(s => s.Category).Distinct().ToList(); // .Distinct to avoid duplicates
        
        filterCategory.Items.Clear();
        filterCategory.Items.Add("All");
        foreach (string cat in categories)
        {
            filterCategory.Items.Add(cat);
        }
        filterCategory.SelectedIndex = 0;

        newCategory.Items.Clear();
        foreach (string cat in categories)
        {
            newCategory.Items.Add(cat);
        }
    }
}