using Serilog;

namespace ChunkFileUploader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("error.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });

            app.MapControllers();

            app.Run();
        }
    }
}
