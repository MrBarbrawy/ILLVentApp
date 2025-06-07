using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Interfaces;
using ILLVentApp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace ILLVentApp.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string AzureBaseUrl = "https://illventapp.azurewebsites.net";

        public ProductService(IAppDbContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<ProductDto>> GetAllProductsAsync()
        {
            var products = await _context.Products
                .AsNoTracking()
                .ToListAsync();

            // Add full URLs to images
            products = products.Select(p => AddFullUrls(p)).ToList();

            return _mapper.Map<List<ProductDto>>(products);
        }

        public async Task<ProductDto> GetProductByIdAsync(int productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return null;

            // Add full URLs to images
            product = AddFullUrls(product);

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<Product> AddProductAsync(ProductDto productDto)
        {
            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Price = productDto.Price,
                ImageUrl = productDto.ImageUrl ?? "",
                Thumbnail = productDto.Thumbnail ?? productDto.ImageUrl ?? "",
                Rating = productDto.Rating,
                ProductType = productDto.ProductType ?? "General",
                HasNFC = productDto.HasNFC,
                HasMedicalDataStorage = productDto.HasMedicalDataStorage,
                HasRescueProtocol = productDto.HasRescueProtocol,
                HasVitalSensors = productDto.HasVitalSensors,
                TechnicalDetails = productDto.TechnicalDetails ?? "",
                StockQuantity = productDto.StockQuantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return product;
        }

        public async Task<Product> UpdateProductAsync(int productId, ProductDto productDto)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return null;

            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Price = productDto.Price;
            product.ProductType = productDto.ProductType ?? "General";
            product.StockQuantity = productDto.StockQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            // Only update image URL if a new one is provided
            if (!string.IsNullOrEmpty(productDto.ImageUrl))
            {
                product.ImageUrl = productDto.ImageUrl;
                product.Thumbnail = productDto.Thumbnail ?? productDto.ImageUrl;
            }

            // Update optional features if provided
            if (productDto.HasNFC != default) product.HasNFC = productDto.HasNFC;
            if (productDto.HasMedicalDataStorage != default) product.HasMedicalDataStorage = productDto.HasMedicalDataStorage;
            if (productDto.HasRescueProtocol != default) product.HasRescueProtocol = productDto.HasRescueProtocol;
            if (productDto.HasVitalSensors != default) product.HasVitalSensors = productDto.HasVitalSensors;
            if (!string.IsNullOrEmpty(productDto.TechnicalDetails)) product.TechnicalDetails = productDto.TechnicalDetails;

            await _context.SaveChangesAsync();

            return product;
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return true;
        }

        private Product AddFullUrls(Product product)
        {
            // Image paths have been updated, so we need to handle the full URLs correctly
            if (!string.IsNullOrEmpty(product.Thumbnail) && !product.Thumbnail.StartsWith("http"))
            {
                product.Thumbnail = $"{AzureBaseUrl}{product.Thumbnail}";
            }
            if (!string.IsNullOrEmpty(product.ImageUrl) && !product.ImageUrl.StartsWith("http"))
            {
                product.ImageUrl = $"{AzureBaseUrl}{product.ImageUrl}";
            }
            return product;
        }
    }
} 