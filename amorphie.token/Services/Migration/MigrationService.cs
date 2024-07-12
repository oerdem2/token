using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.core.Models.Role;
using amorphie.token.data;
using amorphie.token.Services.Role;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Services.Migration
{
    public class MigrationService : ServiceBase, IMigrationService
    {
        private readonly IbDatabaseContext _ibDatabaseContext;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        public MigrationService(IbDatabaseContext ibDatabaseContext, IRoleService roleService, IUserService userService, IConfiguration configuration, ILogger<MigrationService> logger) : base(logger, configuration)
        {
            _ibDatabaseContext = ibDatabaseContext;
            _userService = userService;
            _roleService = roleService;
        }

        public async Task<ServiceResponse> MigrateStaticData()
        {
            var securityImages = await _ibDatabaseContext.SecurityImageDefinition.ToListAsync();
            await _userService.MigrateSecurityImages(securityImages.Select(i => new SecurityImageRequestDto()
            {
                Id = i.Id,
                CreatedAt = i.CreatedAt,
                CreatedBy = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                CreatedByBehalfOf = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                ModifiedAt = i.CreatedAt,
                ModifiedBy = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                ModifiedByBehalfOf = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                EnTitle = i.TitleEn,
                TrTitle = i.TitleTr,
                Image = i.ImagePath
            }).ToList());

            var securityQuestions = await _ibDatabaseContext.QuestionDefinition.Where(d => d.Type == 10).ToListAsync();
            await _userService.MigrateSecurityQuestions(securityQuestions.Select(i => new SecurityQuestionRequestDto()
            {
                Id = i.Id,
                CreatedAt = i.CreatedAt,
                CreatedBy = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                CreatedByBehalfOf = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                ModifiedAt = i.CreatedAt,
                ModifiedBy = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                ModifiedByBehalfOf = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                DescriptionEn = i.DescriptionEn,
                DescriptionTr = i.DescriptionTr,
                IsActive = i.IsActive,
                Key = i.Key,
                Priority = i.Priority,
                ValueTypeClr = i.ValueTypeClr
            }).ToList());

            var roleDefinitions = await _ibDatabaseContext.RoleDefinition.ToListAsync();
            
            var resp = await _roleService.MigrateRoleDefinitions(roleDefinitions.Select(i => new RoleDefinitionDto{
                Description = i.Description!,
                Key = i.Key,
                Id = i.Id,
                Status = i.IsActive.Equals(true) ? "active" : "deactive",
                Tags = ["amorphie"]
            }).ToList());
            Logger.LogInformation("Migrate Role Response Code : " + JsonSerializer.Serialize(resp));
            return new ServiceResponse
            {
                StatusCode = 200
            };
        }

        public async Task<ServiceResponse> MigrateUserData(Guid userId, Guid dodgeUserID)
        {
            var serviceResponse = new ServiceResponse();

            var securityQuestion = await _ibDatabaseContext.Question.Where(q => q.UserId == dodgeUserID)
                .OrderByDescending(q => q.CreatedAt).FirstOrDefaultAsync();

            if (securityQuestion is { })
            {
                var migrateSecurityQuestion = await _userService.MigrateSecurityQuestion(new MigrateSecurityQuestionRequest()
                {
                    Id = securityQuestion.Id,
                    UserId = userId,
                    Answer = securityQuestion.EncryptedAnswer,
                    CreatedAt = securityQuestion.CreatedAt,
                    QuestionStatusType = securityQuestion.Status,
                    SecurityQuestionId = securityQuestion.DefinitionId
                });

                if (migrateSecurityQuestion.StatusCode != 200)
                {
                    return migrateSecurityQuestion;
                }
            }

            var securityImage = await _ibDatabaseContext.SecurityImage.Where(i => i.UserId == dodgeUserID)
                .OrderByDescending(i => i.CreatedAt).FirstOrDefaultAsync();

            if (securityImage is { })
            {
                var migrateSecurityImage = await _userService.MigrateSecurityImage(new MigrateSecurityImageRequest()
                {
                    Id = securityImage.Id,
                    UserId = userId,
                    RequireChange = securityImage.RequireChange,
                    CreatedAt = securityImage.CreatedAt,
                    SecurityImageId = securityImage.DefinitionId
                });

                if (migrateSecurityImage.StatusCode != 200)
                {
                    return migrateSecurityImage;
                }
            }

            serviceResponse.StatusCode = 200;
            return serviceResponse;
        }

    }
}