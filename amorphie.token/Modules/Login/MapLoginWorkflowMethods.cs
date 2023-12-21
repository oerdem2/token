
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace amorphie.token.Modules.Login
{
    public static class MapLoginWorkflowMethods
    {
        public static void MapLoginWorkflowEndpoints(this WebApplication app)
        {
            app.MapPost("/amorphie-login-check-mobile-client", CheckMobileClient.checkMobileClient)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-grant-type", CheckGrantType.checkGrantType)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-scopes", CheckScopes.checkScopes)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-user", CheckUser.checkUser)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-set-login-type", SetLoginType.setLoginType)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-otp-flow", LoginOtpFlow.loginOtpFlow)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-otp-flow", CheckOtpFlow.checkOtpFlow)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-password-change", CheckPasswordChange.checkPasswordChange)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-security-image-change", CheckSecurityImageChange.checkSecurityImageChange)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-security-question-change", CheckSecurityQuestionChange.checkSecurityQuestionChange)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-set-new-password", SetNewPassword.setNewPassword)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-set-new-security-image", SetNewSecurityImage.setNewSecurityImage)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-set-new-security-question", SetNewSecurityQuestion.setNewSecurityQuestion)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-generate-tokens", GenerateTokens.generateTokens)
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/post-transition/{recordId}/{transition}",postTransition);

            static async Task<IResult> postTransition(Guid recordId,string transition,
            [FromBody] dynamic body
            )
            {
                
                
                var httpClient = new HttpClient();  
                StringContent request = new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                request.Headers.Add("User",Guid.NewGuid().ToString());
                var test = JsonSerializer.Serialize(body);
                request.Headers.Add("Behalf-Of-User",Guid.NewGuid().ToString());
                var response = await httpClient.PostAsync(
                    "https://test-amorphie-workflow.burgan.com.tr/workflow/consumer/mobile-login/record/"+recordId+"/transition/"+transition,
                    request);

                return Results.Ok(JsonSerializer.Serialize(await response.Content.ReadAsStringAsync()));
            } 
        }
        
    }
}