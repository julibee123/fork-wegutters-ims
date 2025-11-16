using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WEGutters
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private ObservableCollection<InventoryItemDisplay> _inventoryList;

        public ObservableCollection<InventoryItemDisplay> InventoryList
        {
            get => _inventoryList;
            set
            {
                _inventoryList = value;
                OnPropertyChanged(nameof(InventoryList));
            }
        }

        // notifies when property changes so InventoryList = ... works.
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public MainWindow()
        {            
            InitializeComponent();
            InventoryList = new ObservableCollection<InventoryItemDisplay>();
            InventoryList = DatabaseAccess.GetInventoryItemDisplays();
            this.DataContext = this;
        }

        // Source - https://stackoverflow.com/a
        // Posted by Aleksey
        // Retrieved 2025-11-09, License - CC BY-SA 3.0
        public T FindElementByName<T>(FrameworkElement element, string sChildName) where T : FrameworkElement
        {
            T childElement = null;
            var nChildCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < nChildCount; i++)
            {
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;

                if (child == null)
                    continue;

                if (child is T && child.Name.Equals(sChildName))
                {
                    childElement = (T)child;
                    break;
                }

                childElement = FindElementByName<T>(child, sChildName);

                if (childElement != null)
                    break;
            }
            return childElement;
        }

        #region Stock Button Functionality

        private void Stock_NewItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add logic here to:
            // 1. Edit Item.
            // 2. Update the Stock DataGrid's ItemsSource. !!
            AddEditItem addItemWindow = new AddEditItem(true);
            addItemWindow.Owner = this; // Set this window as the owner
            addItemWindow.Title = "Add New Item";

            // ShowDialog() opens the window and pauses code here until the user closes it
            bool? result = addItemWindow.ShowDialog();

            // Check if the user clicked "Save"
            if (result == true)
            {
                // If they saved, add item to the data grid
                InventoryList.Add(addItemWindow.ReturnItem.ToDisplay());
                addInventoryItem(addItemWindow.ReturnItem);
            }
        }

        #region addMethods 

        private void addNewSKU(SKU sku)
        {
            // add SKU to database and set its ID because AddSKU in database returns the ID 
            sku.SKUID = DatabaseAccess.AddSKU(sku.SKUCode);
        }

        private void addNewCategory(Category category)
        {
            // add Category to database and set its ID because AddCategory in database returns the ID 
            category.CategoryID = DatabaseAccess.AddCategory(category.CategoryName);
        }

        private void addBaseItem(BaseItem baseItem)
        {

            // ensure SKU exists in database else add it
            if (baseItem.SKUProperty.SKUID == 0 && !(DatabaseAccess.SKUExists(baseItem.SKUProperty.SKUCode)) )
            {
                addNewSKU(baseItem.SKUProperty);
            }
            // ensure Category exists in database else add it
            if (baseItem.Category.CategoryID == 0 && !(DatabaseAccess.CategoryExists(baseItem.Category.CategoryName)) )
            {
                addNewCategory(baseItem.Category);
            }

            // add BaseItem to database and set its ID because AddBaseItem in database returns the ID 
            baseItem.ItemID = DatabaseAccess.AddBaseItem(baseItem.SKUProperty, baseItem.ItemName, baseItem.ItemDetails, baseItem.Category,baseItem.Unit, baseItem.QuantityPerBundle);
        }

        private void addInventoryItem(InventoryItem inventoryItem)
        {
            // ensure BaseItem exists in database else add it
            SKU sku = inventoryItem.Item.SKUProperty;
            string itemName = inventoryItem.Item.ItemName;
            string itemDetails = inventoryItem.Item.ItemDetails;
            Category category = inventoryItem.Item.Category;
            string unit = inventoryItem.Item.Unit;
            int qtyPerBundle = inventoryItem.Item.QuantityPerBundle;

            if (inventoryItem.Item.ItemID == 0 && !(DatabaseAccess.BaseItemExists(sku, itemName, itemDetails, category, unit, qtyPerBundle)) )
            {
                addBaseItem(inventoryItem.Item);
            }
            // add InventoryItem to database and set its ID because AddInventoryItem in database returns the ID 
            inventoryItem.InventoryId = DatabaseAccess.AddInventoryItem(inventoryItem.Item, inventoryItem.Quantity, inventoryItem.MinQuantity, inventoryItem.PurchaseCost, inventoryItem.SalePrice, inventoryItem.LastModified, inventoryItem.CreatedDate);
        }

        #endregion

        private void Stock_EditItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add logic here to:
            // 1. Edit Item.
            // 2. Update the Stock DataGrid's ItemsSource.
            var dataGrid = FindElementByName<DataGrid>(ContentControlPanel, "StockDataGrid");
            if (dataGrid?.SelectedItem is InventoryItemDisplay selectedItem)
            {
                AddEditItem addItemWindow = new AddEditItem(false, selectedItem.itemInstance);
                addItemWindow.Owner = this; // Set this window as the owner
                addItemWindow.Title = "Edit Item";

                // ShowDialog() opens the window and pauses code here until the user closes it
                bool? result = addItemWindow.ShowDialog();

                // Check if the user clicked "Save"
                if (result == true)
                {
                    // If they saved, add item to the data grid
                    int selectedIndex = dataGrid.SelectedIndex;
                    InventoryList[selectedIndex] = addItemWindow.ReturnItem.ToDisplay();

                    int newQuantity = addItemWindow.ReturnItem.Quantity;
                    int newMinQuantity = addItemWindow.ReturnItem.MinQuantity;
                    float newPurchaseCost = addItemWindow.ReturnItem.PurchaseCost;
                    float newSalePrice = addItemWindow.ReturnItem.SalePrice;
                    string newLastModified = addItemWindow.ReturnItem.LastModified;

                    InventoryList[selectedIndex].Quantity = newQuantity;
                    
                    SKU sku = addItemWindow.ReturnItem.Item.SKUProperty;
                    string itemName = addItemWindow.ReturnItem.Item.ItemName;
                    string itemDetails = addItemWindow.ReturnItem.Item.ItemDetails;
                    Category category = addItemWindow.ReturnItem.Item.Category;
                    string unit = addItemWindow.ReturnItem.Item.Unit;
                    int qtyPerBundle = addItemWindow.ReturnItem.Item.QuantityPerBundle;

                    if (addItemWindow.ReturnItem.Item.ItemID == 0 && !(DatabaseAccess.BaseItemExists(sku, itemName, itemDetails, category, unit, qtyPerBundle)))
                    {
                        addBaseItem(addItemWindow.ReturnItem.Item);
                    }

                    DatabaseAccess.EditInventoryItem(selectedItem.itemInstance, newQuantity, newMinQuantity, newPurchaseCost, newSalePrice, newLastModified);
                }
            }

        }
        private void Stock_DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add logic here to:
            // 1. Remove Item.
            // 2. Update the Stock DataGrid's ItemsSource.
            var dataGrid = FindElementByName<DataGrid>(ContentControlPanel, "StockDataGrid");
            if (dataGrid?.SelectedItem is InventoryItemDisplay selectedItem)
            {
                InventoryList.Remove(selectedItem);
                DatabaseAccess.DeleteInventoryItem(selectedItem.itemInstance);
            }
        }

        // This method will be called to refresh the data in the Stock DataGrid
        private void Stock_Refresh_Click(object sender, RoutedEventArgs e)
        {
            InventoryList = DatabaseAccess.GetInventoryItemDisplays();
        }

        // This method will handle printing the DataGrid
        private void Stock_Print_Click(object sender, RoutedEventArgs e)
        {
            // Printing is an advanced feature.
            // You will typically use the System.Windows.Controls.PrintDialog class.

            MessageBox.Show("Print Stock Report... (functionality to be added)");
        }

        // This method will save the grid as a PDF
        private void Stock_SaveAsPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Call the ExportToPDF to generate PDF
                bool exported = ExportToPDF.ExportWithSaveDialog(this, InventoryList);

                if (!exported)
                {
                    // Distinguish empty list vs cancelled by user
                    if (InventoryList == null || InventoryList.Count == 0)
                        MessageBox.Show("Nothing to export.", "Export to PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        MessageBox.Show("Export cancelled.", "Export to PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // ExportWithSaveDialog already wrote file; optionally inform user
                    MessageBox.Show("PDF exported.", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to export PDF:\n" + ex.Message, "Export error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // This method will save the grid as an Excel file
        private void Stock_Excel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool exported = ExportToExcel.ExportWithSaveDialog(this, InventoryList);

                if (!exported)
                {
                    // Distinguish empty list vs cancelled by user
                    if (InventoryList == null || InventoryList.Count == 0)
                        MessageBox.Show("Nothing to export.", "Export to Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        MessageBox.Show("Export cancelled.", "Export to Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Excel exported.", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to export Excel:\n" + ex.Message, "Export error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // This method will open the specific "Inventory Count Report"
        private void Stock_InventoryReport_Click(object sender, RoutedEventArgs e)
        {
            // This will likely open a new window or switch to the Reporting tab
            // with a specific report selected.

            MessageBox.Show("Open Inventory Count Report... (functionality to be added)");
        }
        private void StockSearchBox_KeyUp(object sender, KeyEventArgs e)
                {
                    var textBox = sender as TextBox;
                    string searchText = textBox.Text;
                    if (e.Key == Key.Enter)
                    {
                        if (searchText.IsNullOrEmpty())
                        {
                            InventoryList = DatabaseAccess.GetInventoryItemDisplays();
                            textBox.Text = "Search Stock";
                            return;
                        }

                        InventoryList = DatabaseAccess.SearchInventory(searchText);
                    }
                }
        #endregion

            #region Services Button Functionality

        private void Services_Refresh_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add logic to refresh the Services DataGrid
            MessageBox.Show("Refresh Services Data... (functionality to be added)");
        }

        private void Services_NewService_Click(object sender, RoutedEventArgs e)
        {
            AddEditServiceWindow addServiceWindow = new AddEditServiceWindow();
            addServiceWindow.Owner = this; // Set this window as the owner
            addServiceWindow.Title = "Add New Service";

            // ShowDialog() opens the window and pauses code here until the user closes it
            bool? result = addServiceWindow.ShowDialog();
            
            // Check if the user clicked "Save"
            if (result == true)
            {
                // If they saved, refresh the data grid
                Services_Refresh_Click(null, null);
            }
        }

        private void Services_EditService_Click(object sender, RoutedEventArgs e)
        {
            // TODO:
            // 1. Get the selected item from the Services DataGrid
            //    (You'll need to give your DataGrid a name in the template)
            // 2. If nothing is selected, show a message and return.

            // Example:
            // if (myServicesDataGrid.SelectedItem == null)
            // {
            //     MessageBox.Show("Please select a service to edit.");
            //     return;
            // }
            // var selectedService = (YourServiceClass)myServicesDataGrid.SelectedItem;


            AddEditServiceWindow editServiceWindow = new AddEditServiceWindow();
            editServiceWindow.Owner = this;
            editServiceWindow.Title = "Edit Service";

            // TODO:
            // 3. Pre-load the window's textboxes with the selected item's data
            //    editServiceWindow.ItemNameBox.Text = selectedService.Name;
            //    editServiceWindow.CategoryBox.Text = selectedService.Category;
            //    ...etc.

            bool? result = editServiceWindow.ShowDialog();

            if (result == true)
            {
                // If they saved, refresh the data grid
                Services_Refresh_Click(null, null);
            }
        }

        private void Services_DeleteService_Click(object sender, RoutedEventArgs e)
        {
            // TODO:
            // 1. Get the selected item from the Services DataGrid
            // 2. If nothing is selected, return.

            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to delete this service/product?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // TODO:
                // 3. Delete the item from your database
                // 4. Refresh the grid
                Services_Refresh_Click(null, null);
            }
        }

        private void Services_Print_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Print... (functionality to be added)");
        }

        private void Services_SaveAsPdf_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Save as PDF... (functionality to be added)");
        }

        private void Services_PriceTags_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Price Tags... (functionality to be added)");
        }

        private void Services_Sorting_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sorting... (functionality to be added)");
        }


        #endregion

        private void rbStock_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}