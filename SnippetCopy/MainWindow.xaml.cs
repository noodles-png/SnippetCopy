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
using Microsoft.Data.Sqlite;

namespace SnippetCopy;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // All temporary Snippet templates
    private ObservableCollection<Snippet> snippets; // Observable Collection instead of List -> updates the UI automatically
    
    private string savePath = "snippets.db";
    public MainWindow()
    {
        InitializeComponent();
        InitDatabase();
        LoadSnippets();
        snippetList.ItemsSource = snippets;
        UpdateCategories();
    }

    private void InitDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={savePath}");    
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Snippets (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Content TEXT NOT NULL,
                Category TEXT NOT NULL
                )";
        command.ExecuteNonQuery();
    }
    
    // Copy button
    private async void Copy_Click(object sender, RoutedEventArgs e) // WPF needs sender and e, await only with async
    {
        Snippet selected = snippetList.SelectedItem as Snippet;

        if (selected != null)
        {
            Clipboard.SetText(selected.Content);

            var button = sender as Button;
            button.Content = "Copied!";
            await Task.Delay(1000);
            button.Content = "Copy";
        }
    }

    // Shows overview/preview of the selected snippet
    private void SnippetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Snippet selected = snippetList.SelectedItem as Snippet;

        if (selected != null)
        {
            Preview.Text = selected.Content;
            Preview.Visibility = Visibility.Visible;
            newPanel.Visibility = Visibility.Collapsed;
            editSnippet = null;
        }
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        snippetList.SelectedIndex = -1;
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
            string oldName = editSnippet.Name;
            string oldContent = editSnippet.Content;
    
            editSnippet.Name = newName.Text;
            editSnippet.Content = newContent.Text;
            editSnippet.Category = newCategory.Text;
    
            UpdateSnippetInDb(
                new Snippet { Name = oldName, Content = oldContent },
                editSnippet
            );
            editSnippet = null;
        }
        else
        {
            Snippet newSnippet = new Snippet
            {
                Name = newName.Text,
                Content = newContent.Text,
                Category = newCategory.Text
            };
            snippets.Add(newSnippet);
            AddSnippetToDb(newSnippet);
        }
        

        newName.Text = "";
        newContent.Text = "";
        newCategory.SelectedIndex = -1;
        Preview.Visibility = Visibility.Visible;
        newPanel.Visibility = Visibility.Collapsed;
        UpdateCategories();
    }
    
    private Snippet editSnippet;

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        Snippet selected = snippetList.SelectedItem as Snippet;

        if (selected != null)
        {
            snippetList.SelectedIndex = -1;
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
                DeleteSnippetFromDb(selected);
                UpdateCategories(); 
            }
        }
    }

    private void AddSnippetToDb(Snippet snippet)
    {
        using var connection = new SqliteConnection($"Data Source={savePath}");
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Snippets (Name, Content, Category) VALUES (@name, @content, @category)";
        command.Parameters.AddWithValue("@name", snippet.Name);
        command.Parameters.AddWithValue("@content", snippet.Content);
        command.Parameters.AddWithValue("@category", snippet.Category);
        command.ExecuteNonQuery();
    }

    private void DeleteSnippetFromDb(Snippet snippet)
    {
        using var connection = new SqliteConnection($"Data Source={savePath}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"DELETE FROM Snippets WHERE Name = $name AND Content = $content";
        command.Parameters.AddWithValue("$name", snippet.Name);
        command.Parameters.AddWithValue("$content", snippet.Content);
        command.ExecuteNonQuery();
    }

    private void UpdateSnippetInDb(Snippet old, Snippet updated)
    {
        using var connection = new SqliteConnection($"Data Source ={savePath}");
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"UPDATE Snippets 
                                SET Name = $newName, Content = $newContent, Category = $newCategory 
                                WHERE Name = $oldName AND Content = $oldContent";
    }
    
    // Loads existing snippet
    private void LoadSnippets()
    {
        snippets = new ObservableCollection<Snippet>();
        
        using var connection = new SqliteConnection($"Data Source={savePath}");
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"SELECT Name, Content, Category FROM Snippets";
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            snippets.Add(new Snippet
            {
                Name = reader.GetString(0),
                Content = reader.GetString(1),
                Category = reader.GetString(2)
            });
        }
    }
    
    // Segments the snippets into categories
    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (snippets == null || filterCategory.SelectedItem == null) return; 
        
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

    // Search option 
    private void Search_Changed(object sender, TextChangedEventArgs e)
    {
        if (snippets == null || filterCategory.SelectedItem == null) return;
        
        string search = searchBox.Text.ToLower();
        string category = filterCategory.SelectedItem as string;

        var filtered = snippets.Where(s =>
            s.Name.ToLower().Contains(search) || s.Content.ToLower().Contains(search) // || = or
            );
        if (category != "All")
        {
            filtered = filtered.Where(s => s.Category == category);
        }

        snippetList.ItemsSource = filtered.ToList();
    }
}