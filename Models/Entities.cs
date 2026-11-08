using System;

namespace EclatEtSoin.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Client
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public bool IsCounter { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Categorie
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class Fournisseur
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Produit
    {
        public int Id { get; set; }
        public string Barcode { get; set; }
        public string Reference { get; set; }
        public string Designation { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Brand { get; set; }
        public string ImagePath { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal? Pph { get; set; }
        public decimal Discount { get; set; }
        public int Quantity { get; set; }
        public int MinStock { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public int? SupplierId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsLowStock => Quantity > 0 && Quantity <= MinStock;
        public bool IsOutOfStock => Quantity <= 0;
    }

    public class LigneDocument
    {
        public int ProductId { get; set; }
        public string Designation { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal LineTotal => Math.Round(Quantity * UnitPrice * (1 - Discount / 100m), 2);
    }

    public class PharmacySettings
    {
        public string TradeName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string DefaultPrinter { get; set; }
        public int DefaultMinStock { get; set; }
    }
}
