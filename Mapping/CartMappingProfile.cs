using AutoMapper;
using dotnet_backend_2.Data.Entities;
using dotnet_backend_2.DTOs;

namespace dotnet_backend_2.Mapping;

public class CartMappingProfile : Profile
{
    public CartMappingProfile()
    {
        CreateMap<CartItem, CartItemDto>()

        // Flatten product fields
        .ForMember(
            dest => dest.ProductName,
            opt => opt.MapFrom(src => src.Product!.Name)
        )
        .ForMember(
            dest => dest.ProductUrlSlug,
            opt => opt.MapFrom(src => src.Product!.UrlSlug)
        )
        .ForMember(
            dest => dest.ProductPrice,
            opt => opt.MapFrom(src => src.Product!.Price)
        )
        .ForMember(
            dest => dest.ProductImageUrl,
            opt => opt.MapFrom(src => src.Product!.ImageUrl)
        )
        .ForMember(
            dest => dest.LineTotal,
            opt => opt.MapFrom(src => src.Product!.Price * src.Quantity)
        );


        CreateMap<Cart, CartDto>()

        .ForMember(
            dest => dest.TotalItems,
            opt => opt.MapFrom(src => src.Items.Sum(item => item.Quantity))
        )
        .ForMember(
            dest => dest.SubTotal,
            opt => opt.MapFrom(src => src.Items.Sum(item => item.Product!.Price * item.Quantity))
        );

        // FYI: properties with matching names do not need explicit mapping.
    }
}


