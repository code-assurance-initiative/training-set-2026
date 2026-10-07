using ClinicScheduling.Api.Endpoints;
using ClinicScheduling.Api.Security;

namespace ClinicScheduling.Api.Hosting;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseSchedulingApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseHttpsRedirection();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapAppointmentEndpoints();
        app.MapAvailabilityEndpoints();
        app.MapScheduleEndpoints();
        app.MapSeriesEndpoints();
        app.MapPatientEndpoints();
        app.MapControllers();
        return app;
    }
}
