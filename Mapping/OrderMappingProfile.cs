using AutoMapper;
using dotnet_backend_2.Data.Entities;
using dotnet_backend_2.DTOs;

namespace dotnet_backend_2.Mapping;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Address, AddressDto>();

        CreateMap<OrderItem, OrderItemResponseDto>()
            .ForMember(
                dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product!.Name)
            )
            .ForMember(
                dest => dest.ProductUrlSlug,
                opt => opt.MapFrom(src => src.Product!.UrlSlug)
            )
            .ForMember(
                dest => dest.ProductImageUrl,
                opt => opt.MapFrom(src => src.Product!.ImageUrl)
            )
            .ForMember(
                dest => dest.LineTotal,
                opt => opt.MapFrom(src => src.UnitPrice * src.Quantity)
            );

        CreateMap<Order, OrderResponseDto>()
            .ForMember(
                dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString())
            );

        // AutoMapper maps these automatically:
        // - UserId, SessionId, Email, PhoneNumber, ShippingAddress, OrderItems
        // because they share the same names in entity and DTO.
    }
}

