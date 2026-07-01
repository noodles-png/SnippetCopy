using System.Windows;
using System.Windows.Controls;
using SnippetCopy.Models;
using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;

namespace SnippetCopy;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObservableCollection<Snippet> snippets; // Observable Collection instead of List -> updates the UI automatically
    private string savePath = "snippets.db";
    
    /// <summary>
    /// Initializes necessary methods
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        InitDatabase();
        LoadSnippets();
        snippetList.ItemsSource = snippets;
        UpdateCategories();
    }

    /// <summary>
    /// Creates the SQLite database
    /// </summary>
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
    
    /// <summary>
    /// Logic for the copy function via button click with confirmation
    /// </summary>
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
   
    /// <summary>
    /// Displays the content of the selected snippet in the preview panel.
    /// </summary>
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

    /// <summary>
    /// Opens a panel to create a new snippet
    /// </summary>
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

    /// <summary>
    /// Saves the input the user gives in the panel to the SQL database
    /// </summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (editSnippet != null)
        {
            string oldName = editSnippet.Name;
            string oldContent = editSnippet.Content;
    
            editSnippet.Name = newName.Text;
            editSnippet.Content = newContent.Text;
            editSnippet.Category = newCategory.Text;

            UpdateSnippetInDb(editSnippet);
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

    /// <summary>
    /// Updates existing snippets with new values
    /// </summary>
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
    
    /// <summary>
    /// Removes selected from the SQL database
    /// </summary>
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

    /// <summary>
    /// Logic behind saving to the database 
    /// </summary>
    private void AddSnippetToDb(Snippet snippet)
    {
        using var connection = new SqliteConnection($"Data Source={savePath}");
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Snippets (Name, Content, Category) VALUES (@name, @content, @category);
                                SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@name", snippet.Name);
        command.Parameters.AddWithValue("@content", snippet.Content);
        command.Parameters.AddWithValue("@category", snippet.Category);

        snippet.Id = Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Logic to remove snippets from the database
    /// </summary>
    private void DeleteSnippetFromDb(Snippet snippet)
    {
        using var connection = new SqliteConnection($"Data Source={savePath}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"DELETE FROM Snippets WHERE Id = $id";
        command.Parameters.AddWithValue("$id", snippet.Id);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Replaces old values with new values in the database
    /// </summary>
    private void UpdateSnippetInDb(Snippet snippet)
    {
        using var connection = new SqliteConnection($"Data Source ={savePath}");
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"UPDATE Snippets 
                                SET Name = $Name, Content = $Content, Category = $Category 
                                WHERE Id = $id";
        command.Parameters.AddWithValue("$id", snippet.Id);
        command.Parameters.AddWithValue("$Name", snippet.Name);
        command.Parameters.AddWithValue("$Content", snippet.Content);
        command.Parameters.AddWithValue("$Category", snippet.Category);
        command.ExecuteNonQuery();
    }
    
    /// <summary>
    /// Calls at startup the database to populate the displayed value
    /// </summary>
    private void LoadSnippets()
    {
        snippets = new ObservableCollection<Snippet>();
        
        using var connection = new SqliteConnection($"Data Source={savePath}");
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"SELECT Id, Name, Content, Category FROM Snippets";
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            snippets.Add(new Snippet
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Content = reader.GetString(2),
                Category = reader.GetString(3)
            });
        }
    }
    
    /// <summary>
    /// Displays snippets according to category filter
    /// </summary>

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
    
    /// <summary>
    /// Logic for ComboBox lists to display categories in real time
    /// </summary>
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

    /// <summary>
    /// Logic for ComboBox lists to display snippets filtered by fitting strings in name and content
    /// </summary>
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