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
            // This method will be implemented in admin dashboard later
            throw new NotImplementedException("Method will be implemented in admin dashboard");
        }

        public async Task<Product> UpdateProductAsync(int productId, ProductDto productDto)
        {
            // This method will be implemented in admin dashboard later
            throw new NotImplementedException("Method will be implemented in admin dashboard");
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            // This method will be implemented in admin dashboard later
            throw new NotImplementedException("Method will be implemented in admin dashboard");
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