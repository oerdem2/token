
using System.Dynamic;
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
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-grant-type", CheckGrantType.checkGrantType)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-scopes", CheckScopes.checkScopes)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-user", CheckUser.checkUser)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-disable-user", DisableUser.disableUser)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-set-login-type", SetLoginType.setLoginType)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-otp-flow", LoginOtpFlow.loginOtpFlow)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-otp-flow", CheckOtpFlow.checkOtpFlow)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-clear-otp-flow", ClearOtpFlow.clearOtpFlow)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-password-change", CheckPasswordChange.checkPasswordChange)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-security-image-change", CheckSecurityImageChange.checkSecurityImageChange)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-security-question-change", CheckSecurityQuestionChange.checkSecurityQuestionChange)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-set-new-password", SetNewPassword.setNewPassword)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-set-new-security-image", SetNewSecurityImage.setNewSecurityImage)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-set-new-security-question", SetNewSecurityQuestion.setNewSecurityQuestion)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-check-documents", CheckDocuments.checkDocuments)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapPost("/amorphie-login-generate-tokens", GenerateTokens.generateTokens)
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

            app.MapGet("/amorphie-login-test", () =>
            {
                dynamic dd = new ExpandoObject();
                var t = dd.GetProperty("test");
                return Results.Ok();
            })
            .ExcludeFromDescription()
            .Produces(StatusCodes.Status200OK);

        }



    }
}