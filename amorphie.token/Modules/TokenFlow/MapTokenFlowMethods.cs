

namespace amorphie.token.Modules.OtpProcess
{
    public static class MapTokenFlowMethods
    {
        public static void MapTokenFlowEndpoints(this WebApplication app)
        {

            app.MapGet("/filter-test", () => {
                Console.WriteLine("Method executed");
            })
            .AddEndpointFilter(async(context,next) => {
                var result = await next(context);
                return result;
            })
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);
            
            
        }



    }
}