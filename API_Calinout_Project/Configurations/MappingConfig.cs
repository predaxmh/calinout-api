using API_Calinout_Project.DTOs;
using API_Calinout_Project.Entities;
using Mapster;

namespace API_Calinout_Project.Configurations
{
    public static class MappingConfig
    {
        public static void Configure()
        {
            // 1. Request -> Entity
            TypeAdapterConfig<CreateFoodTypeRequest, FoodType>
                .NewConfig()
                .Map(dest => dest.CreatedAt, src => DateTime.UtcNow) // Force server-time
                .Map(dest => dest.UserId, src => "")
                .Ignore(dest => dest.Id); // ID is DB generated

            // 2. Entity -> Response
            TypeAdapterConfig<FoodType, FoodTypeResponse>
                .NewConfig(); // Simple 1-to-1 map

            TypeAdapterConfig<UpdateFoodTypeRequest, FoodType>
                .NewConfig()
                .IgnoreNullValues(true)
                .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);

            //// Daily log

            // 1. Request -> Entity
            TypeAdapterConfig<CreateDailyLogRequest, DailyLog>
                .NewConfig()
                .Map(dest => dest.CreatedAt, src => DateTime.UtcNow) // Force server-time
                .Ignore(dest => dest.Id); // ID is DB generated

            // 2. Entity -> Response
            TypeAdapterConfig<DailyLog, DailyLogResponse>
                .NewConfig(); // Simple 1-to-1 map

            TypeAdapterConfig<UpdateDailyLogRequest, DailyLog>
                .NewConfig()
                .IgnoreNullValues(true)
                .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);


            // food
            //locally done
            // 1. Request -> Entity
            TypeAdapterConfig<CreateFoodRequest, Food>
                .NewConfig()
                .Map(dest => dest.CreatedAt, src => DateTime.UtcNow) // Force server-time
                .Ignore(dest => dest.Id); // ID is DB generated


            // 2. Entity -> Response
            TypeAdapterConfig<Food, FoodResponse>
                .NewConfig(); // Simple 1-to-1 map

            TypeAdapterConfig<UpdateFoodRequest, Food>
                .NewConfig()
                .IgnoreNullValues(true)
                .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);


            //// meal

            // 1. Request -> Entity
            TypeAdapterConfig<CreateMealRequest, Meal>
                .NewConfig()
                .Map(dest => dest.CreatedAt, src => DateTime.UtcNow) // Force server-time
                .Ignore(dest => dest.Id); // ID is DB generated

            // 2. Entity -> Response
            TypeAdapterConfig<Meal, MealResponse>
                .NewConfig(); // Simple 1-to-1 map

            TypeAdapterConfig<UpdateMealRequest, Meal>
                .NewConfig()
                .IgnoreNullValues(true)
                .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);
        }
    }
}