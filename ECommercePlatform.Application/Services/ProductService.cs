using AutoMapper;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Application.Interfaces.IProduct;
using ECommercePlatform.Domain.Entities;

namespace ECommercePlatform.Application.Services
{
    public class ProductService(IUnitOfWork unitOfWork, IMapper mapper) : IProductService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;

        public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
        {
            var product = _mapper.Map<Product>(createProductDto);
            product.IsAvailable = true;
            product.CreatedOn = DateTime.Now;
            product.IsActive = true;

            var result = await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<ProductDto>(result);
        }

        public async Task DeleteProductAsync(Guid id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Product with ID {id} not found");

            // Soft delete
            product.IsDeleted = true;
            product.IsActive = false;
            product.ModifiedOn = DateTime.Now;

            await _unitOfWork.Products.UpdateAsync(product);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IReadOnlyList<ProductDto>> GetAllProductsAsync()
        {
            var products = await _unitOfWork.Products.FindAsync(p => p.IsActive && !p.IsDeleted);
            return _mapper.Map<IReadOnlyList<ProductDto>>(products);
        }

        public async Task<ProductDto> GetProductByIdAsync(Guid id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null || product.IsDeleted)
                throw new KeyNotFoundException($"Product with ID {id} not found");

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<IReadOnlyList<ProductDto>> GetProductsByCategoryAsync(Guid categoryId)
        {
            var products = await _unitOfWork.Products.GetProductsByCategoryAsync(categoryId);
            return _mapper.Map<IReadOnlyList<ProductDto>>(products);
        }

        public async Task<ProductDto> GetProductWithDetailsAsync(Guid id)
        {
            var product = await _unitOfWork.Products.GetProductWithDetailsAsync(id);
            if (product == null || product.IsDeleted)
                throw new KeyNotFoundException($"Product with ID {id} not found");

            return _mapper.Map<ProductDto>(product);
        }

        public async Task UpdateProductAsync(Guid id, UpdateProductDto updateProductDto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null || product.IsDeleted)
                throw new KeyNotFoundException($"Product with ID {id} not found");

            _mapper.Map(updateProductDto, product);
            product.ModifiedOn = DateTime.Now;

            await _unitOfWork.Products.UpdateAsync(product);
            await _unitOfWork.CompleteAsync();
        }
    }
}