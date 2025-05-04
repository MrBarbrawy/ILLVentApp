using System.Collections.Generic;
using System.Threading.Tasks;
using ILLVentApp.Domain.DTOs;
using ILLVentApp.Domain.Models;

namespace ILLVentApp.Domain.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllProductsAsync();
        Task<ProductDto> GetProductByIdAsync(int productId);
        Task<Product> AddProductAsync(ProductDto productDto);
        Task<Product> UpdateProductAsync(int productId, ProductDto productDto);
        Task<bool> DeleteProductAsync(int productId);
    }
} 